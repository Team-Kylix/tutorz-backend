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
using System.Text.Json;
using System.Net.Http;

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
        private readonly HttpClient _httpClient;

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
            _httpClient = new HttpClient();
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
                // mapped all fields from the request
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
                // Validate required fields for Institute
                if (string.IsNullOrWhiteSpace(request.InstituteName) || string.IsNullOrWhiteSpace(request.Address))
                {
                    throw new Exception("Institute Name and Address are required.");
                }

                await _instituteRepository.AddAsync(new Institute
                {
                    UserId = user.UserId,
                    InstituteName = request.InstituteName ?? request.FirstName,
                    Address = request.Address,
                    ContactNumber = request.PhoneNumber 
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

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            var user = await _userRepository.GetAsync(u => u.Email == email);
            return user != null;
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

        // Validate Access Token via Google API
        private async Task<SocialLoginUser> ValidateGoogleAccessTokenAsync(string accessToken)
        {
            try
            {
                // Call Google's UserInfo endpoint with the Access Token
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Google token validation failed. Token might be expired or invalid.");
                }

                var content = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    throw new Exception("Could not retrieve email from Google.");
                }

                return new SocialLoginUser
                {
                    Email = googleUser.Email,
                    Name = googleUser.Name
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Google validation error: {ex.Message}");
            }
        }

        // Helper classes for JSON deserialization
        private class GoogleUserInfo
        {
            public string Sub { get; set; }
            public string Name { get; set; }
            public string Given_Name { get; set; }
            public string Family_Name { get; set; }
            public string Picture { get; set; }
            public string Email { get; set; }
            public bool Email_Verified { get; set; }
        }

        private class SocialLoginUser
        {
            public string Email { get; set; }
            public string Name { get; set; }
        }

        public async Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request)
        {
            SocialLoginUser socialUser;
            if (request.Provider.ToLower() == "google")
            {
                // We pass the "IdToken" from the frontend, which is actually an Access Token (ya29...)
                socialUser = await ValidateGoogleAccessTokenAsync(request.IdToken);
            }
            else
            {
                throw new Exception("Invalid provider");
            }

            // Check if user exists
            var user = await _userRepository.GetAsync(u => u.Email == socialUser.Email);
            var roleName = "";
            bool isNewUser = false;

            if (user == null)
            {
                isNewUser = true;

                if (string.IsNullOrEmpty(request.Role))
                {
                    throw new Exception("Role is required for new social login users.");
                }

                // Validate Phone Number
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    string normalizedPhone = "+94" + request.PhoneNumber.Substring(1);
                    if (await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone) != null)
                    {
                        throw new Exception("This phone number is already in use.");
                    }
                }

                var role = await _roleRepository.GetAsync(r => r.Name == request.Role);
                if (role == null) throw new Exception($"Invalid role '{request.Role}' for new user.");

                roleName = role.Name;

                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = socialUser.Email,
                    PasswordHash = "",
                    RoleId = role.RoleId,
                    PhoneNumber = !string.IsNullOrEmpty(request.PhoneNumber)
                                  ? ("+94" + request.PhoneNumber.Substring(1))
                                  : null
                };

                await _userRepository.AddAsync(user);

                // Map Profile Details
                if (roleName == "Tutor")
                {
                    await _tutorRepository.AddAsync(new Tutor
                    {
                        UserId = user.UserId,
                        FirstName = request.FirstName ?? socialUser.Name.Split(' ')[0],
                        LastName = request.LastName ?? (socialUser.Name.Contains(' ') ? socialUser.Name.Split(' ')[1] : ""),
                        Bio = request.Bio,
                        BankAccountNumber = request.BankAccountNumber,
                        BankName = request.BankName
                    });
                }
                else if (roleName == "Student")
                {
                    await _studentRepository.AddAsync(new Student
                    {
                        UserId = user.UserId,
                        FirstName = request.FirstName ?? socialUser.Name.Split(' ')[0],
                        LastName = request.LastName ?? (socialUser.Name.Contains(' ') ? socialUser.Name.Split(' ')[1] : ""),
                        SchoolName = request.SchoolName,
                        Grade = request.Grade,
                        ParentName = request.ParentName,
                        DateOfBirth = request.DateOfBirth ?? DateTime.UtcNow
                    });
                }
                else if (roleName == "Institute")
                {
                    await _instituteRepository.AddAsync(new Institute
                    {
                        UserId = user.UserId,
                        InstituteName = request.InstituteName ?? socialUser.Name,
                        Address = request.Address,
                        ContactNumber = user.PhoneNumber
                    });
                }
            }
            else
            {
                var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
                if (role == null) throw new Exception("Role not found for existing user.");
                roleName = role.Name;
            }

            if (isNewUser)
            {
                await _userRepository.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user, roleName);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = roleName,
                Token = token
            };
        }

    }
}
