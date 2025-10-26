using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
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

        // --- 1. ADD THE INSTITUTE REPOSITORY ---
        private readonly IInstituteRepository _instituteRepository;

        private readonly IConfiguration _configuration;
        private readonly IRoleRepository _roleRepository;

        public AuthService(
            IUserRepository userRepository,
            ITutorRepository tutorRepository,
            IStudentRepository studentRepository,

            // --- 2. ADD IT TO THE CONSTRUCTOR ---
            IInstituteRepository instituteRepository,

            IRoleRepository roleRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _studentRepository = studentRepository;

            // --- 3. ASSIGN IT ---
            _instituteRepository = instituteRepository;

            _roleRepository = roleRepository;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user exists (using the repository!)
            if (await _userRepository.GetAsync(u => u.Email == request.Email) != null)
            {
                throw new Exception("User with this email already exists.");
            }

            // Get the Role from the database
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
                PhoneNumber = request.PhoneNumber
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
                // 👇 YOU WILL ADD STUDENT LOGIC HERE LATER
                // (e.g., FullName, SchoolName)
                await _studentRepository.AddAsync(new Student
                {
                    UserId = user.UserId,
                    // FullName = request.FullName,
                    // SchoolName = request.SchoolName
                });
            }
            else if (request.Role == "Institute")
            {
                // 👇 YOU WILL ADD INSTITUTE LOGIC HERE LATER
                // (e.g., InstituteName)
                await _instituteRepository.AddAsync(new Institute
                {
                    UserId = user.UserId,
                    // InstituteName = request.InstituteName
                });
            }

            await _userRepository.SaveChangesAsync();

            // Generate token (NOW WITH THE ROLE)
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
            // 1. Find user
            var user = await _userRepository.GetAsync(u => u.Email == request.Email);

            // 2. Validate
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password.");
            }

            // 3. GET THE ROLE (This is the new part)
            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
            if (role == null)
            {
                throw new Exception("User has no valid role.");
            }

            // 4. Generate token (pass the role name)
            var token = GenerateJwtToken(user, role.Name);

            // 5. Populate the response
            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token
            };
        }

        // private string GenerateJwtToken(User user)
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
            // --- START: Removed Duplicate Variable ---
            // This line was removed: SocialLoginUser socialUser = await ValidateGoogleTokenAsync(request.IdToken);
            // --- END: Removed Duplicate Variable ---

            // 1. Validate the external token and get user info
            SocialLoginUser socialUser; // Declare the variable once here
            if (request.Provider.ToLower() == "google")
            {
                socialUser = await ValidateGoogleTokenAsync(request.IdToken);
            }
            else
            {
                throw new Exception("Invalid provider");
            }

            // 2. Find or create the user in your database
            var user = await _userRepository.GetAsync(u => u.Email == socialUser.Email);
            var roleName = "";
            bool isNewUser = false; // Flag to check if we created a user

            if (user == null) // New user
            {
                isNewUser = true; // Mark as new user

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
                // *** IMPORTANT: You need to implement the actual creation logic here ***
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
                // else if (roleName == "Institute") { /* await _instituteRepository.AddAsync(new Institute { UserId = user.UserId }); */ } // Uncomment when InstituteRepo is ready

            }
            else // Existing user
            {
                var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
                // Handle cases where role might not be found (though unlikely if data is consistent)
                if (role == null)
                {
                    throw new Exception($"Role not found for existing user with RoleId: {user.RoleId}");
                }
                roleName = role.Name;
            }

            // Save changes ONLY IF a new user or profile was created
            if (isNewUser)
            {
                await _userRepository.SaveChangesAsync(); // Use the context's SaveChangesAsync via one repository
                Console.WriteLine($"DEBUG: Saved changes for new user: {user.Email}"); // Add logging
            }


            // 3. Issue your OWN token
            var token = GenerateJwtToken(user, roleName);

            // --- START: Populate the Response Correctly ---
            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = roleName, // Use the roleName we determined
                Token = token
            };
            // --- END: Populate the Response Correctly ---
        }

        //validation logic for google token
        private async Task<SocialLoginUser> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                // 1. Get your Client ID from the configuration
                var googleClientId = _configuration["Google:ClientId"];
                if (string.IsNullOrEmpty(googleClientId))
                {
                    throw new Exception("Google ClientId is not configured.");
                }

                // 2. Set up the validation settings
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };

                // 3. Validate the token
                // This method contacts Google's servers to verify the token is real
                // and was issued for your application.
                GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

                // 4. Return the user info
                return new SocialLoginUser
                {
                    Email = payload.Email,
                    Name = payload.Name
                };
            }
            catch (Exception ex)
            {
                // This will catch invalid tokens, expired tokens, etc.
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
