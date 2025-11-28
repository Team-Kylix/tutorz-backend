using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions; // Import Regex
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Auth;
using Tutorz.Domain.Entities;
using BCrypt.Net; // For BCrypt
using Google.Apis.Auth;

namespace Tutorz.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IInstituteRepository _instituteRepository;
        private readonly IConfiguration _configuration;
        private readonly IRoleRepository _roleRepository;

        // Constructor...
        public AuthService(
            IUserRepository userRepository,
            ITutorRepository tutorRepository,
            IStudentRepository studentRepository,
            IInstituteRepository instituteRepository,
            IRoleRepository roleRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _studentRepository = studentRepository;
            _instituteRepository = instituteRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Phone Number Validation 
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                throw new Exception("Phone number is required for registration.");
            }
            if (!Regex.IsMatch(request.PhoneNumber, @"^07\d{8}$"))
            {
                throw new Exception("Invalid phone number format. Must be 10 digits starting with 07 (e.g., 0712345678).");
            }
            string normalizedPhone = "+94" + request.PhoneNumber.Substring(1);
            if (await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone) != null)
            {
                throw new Exception("This phone number is already registered. Please log in.");
            }

            // Email Validation
            if (await _userRepository.GetAsync(u => u.Email == request.Email) != null)
            {
                throw new Exception("User with this email already exists.");
            }

            // Role Validation
            var role = await _roleRepository.GetAsync(r => r.Name == request.Role);
            if (role == null)
            {
                throw new Exception($"Role '{request.Role}' does not exist.");
            }

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = role.RoleId,
                PhoneNumber = normalizedPhone
            };
            await _userRepository.AddAsync(user);

            if (request.Role == "Tutor")
            {
                await _tutorRepository.AddAsync(new Tutor
                {
                    UserId = user.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Bio = request.Bio,
                    BankAccountNumber = request.BankAccountNumber,
                    BankName = request.BankName,
                    ExperienceYears = request.ExperienceYears
                });
            }
            else if (request.Role == "Student")
            {
                // Uncommented and mapped all fields from the request
                await _studentRepository.AddAsync(new Student
                {
                    UserId = user.UserId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    SchoolName = request.SchoolName,
                    Grade = request.Grade,
                    ParentName = request.ParentName,
                    DateOfBirth = request.DateOfBirth ?? DateTime.UtcNow 
                });
            }
            else if (request.Role == "Institute")
            {
                await _instituteRepository.AddAsync(new Institute
                {
                    UserId = user.UserId,
                    InstituteName = request.FirstName 
                });
            }

            await _userRepository.SaveChangesAsync();

            var token = GenerateJwtToken(user, role.Name);
            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token
            };
        }
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            User user = null;
            string searchIdentifier = request.Identifier;

            // Determine if input is Email or Phone
            bool isEmail = searchIdentifier.Contains("@");

            if (isEmail)
            {
                // Search by Email
                user = await _userRepository.GetAsync(u => u.Email == searchIdentifier);
            }
            else
            {
                // Logic for Phone Number
                // Remove any spaces or dashes
                string cleanPhone = searchIdentifier.Replace(" ", "").Replace("-", "");

                // If user entered 0712345678, convert to +94712345678 to match database format
                if (cleanPhone.StartsWith("0"))
                {
                    cleanPhone = "+94" + cleanPhone.Substring(1);
                }
                else if (!cleanPhone.StartsWith("+"))
                {
                    // If they just typed 712345678, add +94
                    cleanPhone = "+94" + cleanPhone;
                }

                // Search by Phone
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            // Validation
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Generic error message for security
                throw new Exception("Invalid email/mobile number or password.");
            }

            // Get Role and Return Token (Existing logic)
            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
            if (role == null) throw new Exception("User has no valid role.");

            var token = GenerateJwtToken(user, role.Name);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token
            };
        }

        // private string GenerateJwtToken
        private string GenerateJwtToken(User user, string roleName) 
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        // Use the roleName variable
        new Claim(ClaimTypes.Role, roleName)
    };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request)
        {

            // Validate the external token and get user info
            SocialLoginUser socialUser;
            if (request.Provider.ToLower() == "google")
            {
                socialUser = await ValidateGoogleTokenAsync(request.IdToken);
            }
            else
            {
                throw new Exception("Invalid provider");
            }

            // Find or create the user in your database
            var user = await _userRepository.GetAsync(u => u.Email == socialUser.Email);
            var roleName = "";
            bool isNewUser = false;

            if (user == null) 
            {
                isNewUser = true; 

                // Ensure the role was provided in the request for new users
                if (string.IsNullOrEmpty(request.Role))
                {
                    throw new Exception("Role is required for new social login users.");
                }

                var role = await _roleRepository.GetAsync(r => r.Name == request.Role);
                if (role == null) throw new Exception($"Invalid role '{request.Role}' for new user.");
                roleName = role.Name;

                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = socialUser.Email,
                    // PasswordHash is null for social logins
                    RoleId = role.RoleId
                };
                await _userRepository.AddAsync(user);

                // Create the specific profile based on the role
                if (roleName == "Tutor")
                {
                    await _tutorRepository.AddAsync(new Tutor { UserId = user.UserId });
                    Console.WriteLine($"DEBUG: Created Tutor profile for User ID: {user.UserId}"); // Add logging
                }
                else if (roleName == "Student")
                {
                    await _studentRepository.AddAsync(new Student { UserId = user.UserId });
                    Console.WriteLine($"DEBUG: Created Student profile for User ID: {user.UserId}"); // Add logging
                }
                // else if (roleName == "Institute") { /* await _instituteRepository.AddAsync(new Institute { UserId = user.UserId }); */ }

            }
            else // Existing user
            {
                var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
                // Handle cases where role might not be found
                if (role == null)
                {
                    throw new Exception($"Role not found for existing user with RoleId: {user.RoleId}");
                }
                roleName = role.Name;
            }

            // Save changes ONLY IF a new user or profile was created
            if (isNewUser)
            {
                await _userRepository.SaveChangesAsync(); // Use the context SaveChangesAsync via one repository
                Console.WriteLine($"DEBUG: Saved changes for new user: {user.Email}"); // Add logging
            }


            // Issue your OWN token
            var token = GenerateJwtToken(user, roleName);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = roleName, 
                Token = token
            };
        }

        // validation logic for google token
        private async Task<SocialLoginUser> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                // Get your Client ID from the configuration
                var googleClientId = _configuration["Google:ClientId"];
                if (string.IsNullOrEmpty(googleClientId))
                {
                    throw new Exception("Google ClientId is not configured.");
                }

                // Set up the validation settings
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };

                // Validate the token
                // This method contacts Google's servers to verify the token is real
                // and was issued for your application.
                GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

                // Return the user info
                return new SocialLoginUser
                {
                    Email = payload.Email,
                    Name = payload.Name
                };
            }
            catch (Exception ex)
            {
                // This will catch invalid tokens, expired tokens.
                throw new Exception("Google token validation failed.", ex);
            }
        }

        // A helper class
        private class SocialLoginUser
        {
            public string Email { get; set; }
            public string Name { get; set; }
        }


    }
}
