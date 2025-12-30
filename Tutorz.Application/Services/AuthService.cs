using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Auth;
using Tutorz.Domain.Entities;
using BCrypt.Net;
using Google.Apis.Auth;
using System.Text.Json;
using System.Net.Http;
using System.Security.Cryptography;

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
        private readonly IEmailService _emailService;
        private readonly IIdGeneratorService _idGeneratorService;

        public AuthService(
            IUserRepository userRepository,
            ITutorRepository tutorRepository,
            IStudentRepository studentRepository,
            IInstituteRepository instituteRepository,
            IRoleRepository roleRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IIdGeneratorService idGeneratorService)
        {
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _studentRepository = studentRepository;
            _instituteRepository = instituteRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
            _emailService = emailService;
            _idGeneratorService = idGeneratorService;
            _httpClient = new HttpClient();
        }

        // --- REGISTER (First Time User) ---
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (request.Password.Length < 6 || request.Password.Length > 10)
                throw new Exception("Password must be between 6 and 10 characters.");

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new Exception("Phone number is required for registration.");

            if (!Regex.IsMatch(request.PhoneNumber, @"^07\d{8}$"))
                throw new Exception("Invalid phone number format. Must be 10 digits starting with 07 (e.g., 0712345678).");

            string normalizedPhone = "+94" + request.PhoneNumber.Substring(1);
            if (await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone) != null)
                throw new Exception("This phone number is already registered. Please log in.");

            if (await _userRepository.GetAsync(u => u.Email == request.Email) != null)
                throw new Exception("User with this email already exists.");

            var role = await _roleRepository.GetAsync(r => r.Name == request.Role);
            if (role == null) throw new Exception($"Role '{request.Role}' does not exist.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = role.RoleId,
                PhoneNumber = normalizedPhone
            };
            await _userRepository.AddAsync(user);

            string customId = await _idGeneratorService.GenerateNextIdAsync(request.Role, request.Grade);
            Guid? newStudentId = null;

            if (request.Role == "Tutor")
            {
                await _tutorRepository.AddAsync(new Tutor
                {
                    UserId = user.UserId,
                    RegistrationNumber = customId,
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
                var student = new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user.UserId,
                    RegistrationNumber = customId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    SchoolName = request.SchoolName,
                    Grade = request.Grade,
                    ParentName = request.ParentName,
                    DateOfBirth = request.DateOfBirth ?? DateTime.UtcNow,
                    IsPrimary = true
                };
                newStudentId = student.StudentId;
                await _studentRepository.AddAsync(student);
            }
            else if (request.Role == "Institute")
            {
                if (string.IsNullOrWhiteSpace(request.InstituteName) || string.IsNullOrWhiteSpace(request.Address))
                    throw new Exception("Institute Name and Address are required.");

                await _instituteRepository.AddAsync(new Institute
                {
                    UserId = user.UserId,
                    RegistrationNumber = customId,
                    InstituteName = request.InstituteName ?? request.FirstName,
                    Address = request.Address,
                    ContactNumber = request.PhoneNumber
                });
            }

            await _userRepository.SaveChangesAsync();

            var token = GenerateJwtToken(user, role.Name, newStudentId);

            var response = new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = newStudentId,
                Profiles = new List<StudentProfileDto>(),
                // ✅ Correctly return the generated info
                FirstName = request.Role == "Institute" ? (request.InstituteName ?? request.FirstName) : request.FirstName,
                LastName = request.LastName,
                RegistrationNumber = customId
            };

            if (newStudentId.HasValue)
            {
                response.Profiles.Add(new StudentProfileDto
                {
                    StudentId = newStudentId.Value,
                    FirstName = request.FirstName,
                    Grade = request.Grade,
                    IsPrimary = true
                });
            }

            return response;
        }

        // --- LOGIN (Fixed to return RegistrationNumber) ---
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            if (request.Password.Length < 6 || request.Password.Length > 10)
                throw new Exception("Password must be between 6 and 10 characters.");

            User user = null;
            string searchIdentifier = request.Identifier;
            bool isEmail = searchIdentifier.Contains("@");

            if (isEmail)
            {
                user = await _userRepository.GetAsync(u => u.Email == searchIdentifier);
            }
            else
            {
                string cleanPhone = searchIdentifier.Replace(" ", "").Replace("-", "");
                if (cleanPhone.StartsWith("0")) cleanPhone = "+94" + cleanPhone.Substring(1);
                else if (!cleanPhone.StartsWith("+")) cleanPhone = "+94" + cleanPhone;

                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            if (user == null) throw new Exception("Invalid email/mobile number or password.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("This account was created using Google. Please use the 'Continue with Google' button.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid email/mobile number or password.");

            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
            if (role == null) throw new Exception("User has no valid role.");

            List<StudentProfileDto> profiles = new();
            Guid? currentStudentId = null;

            // ✅ Initialize variables to hold specific user info
            string firstName = "";
            string lastName = "";
            string regNo = "";

            // ✅ Logic to fetch RegistrationNumber based on role
            if (role.Name == "Tutor")
            {
                var tutor = await _tutorRepository.GetAsync(t => t.UserId == user.UserId);
                if (tutor != null)
                {
                    firstName = tutor.FirstName;
                    lastName = tutor.LastName;
                    regNo = tutor.RegistrationNumber;
                }
            }
            else if (role.Name == "Institute")
            {
                var inst = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                if (inst != null)
                {
                    firstName = inst.InstituteName;
                    regNo = inst.RegistrationNumber;
                }
            }
            else if (role.Name == "Student")
            {
                var students = await _studentRepository.GetAllAsync(s => s.UserId == user.UserId);
                if (students.Any())
                {
                    profiles = students.Select(s => new StudentProfileDto
                    {
                        StudentId = s.StudentId,
                        FirstName = s.FirstName,
                        Grade = s.Grade,
                        IsPrimary = s.IsPrimary
                    }).ToList();

                    var activeStudent = students.FirstOrDefault(s => s.IsPrimary) ?? students.FirstOrDefault();
                    if (activeStudent != null)
                    {
                        currentStudentId = activeStudent.StudentId;
                        firstName = activeStudent.FirstName;
                        lastName = activeStudent.LastName;
                        regNo = activeStudent.RegistrationNumber;
                    }
                }
            }

            var token = GenerateJwtToken(user, role.Name, currentStudentId);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = currentStudentId,
                Profiles = profiles,
                // ✅ Return fetched details
                FirstName = firstName,
                LastName = lastName,
                RegistrationNumber = regNo
            };
        }

        public async Task<AuthResponse> SwitchProfileAsync(Guid userId, Guid targetStudentId)
        {
            var student = await _studentRepository.GetAsync(s => s.StudentId == targetStudentId && s.UserId == userId);

            if (student == null)
                throw new Exception("Student profile not found or does not belong to this user.");

            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);

            var token = GenerateJwtToken(user, role.Name, student.StudentId);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = student.StudentId,
                Profiles = new List<StudentProfileDto>(),
                // ✅ Update info for the switched profile
                FirstName = student.FirstName,
                LastName = student.LastName,
                RegistrationNumber = student.RegistrationNumber
            };
        }

        public async Task<string> CheckUserStatusAsync(string identifier)
        {
            User user;
            if (identifier.Contains("@"))
                user = await _userRepository.GetAsync(u => u.Email == identifier);
            else
            {
                string cleanPhone = identifier.StartsWith("0") ? "+94" + identifier.Substring(1) : identifier;
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            if (user == null) return "NOT_FOUND";

            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);

            if (role.Name == "Student") return "EXISTS_AS_STUDENT";

            return "EXISTS_OTHER_ROLE";
        }

        public async Task<AuthResponse> RegisterSiblingAsync(SiblingRegistrationRequest request)
        {
            string searchIdentifier = request.Identifier;

            if (!searchIdentifier.Contains("@") && searchIdentifier.StartsWith("0"))
            {
                searchIdentifier = "+94" + searchIdentifier.Substring(1);
            }

            var user = await _userRepository.GetAsync(u => u.Email == searchIdentifier || u.PhoneNumber == searchIdentifier);
            if (user == null) throw new Exception("Parent account not found.");

            string newStudentId = await _idGeneratorService.GenerateNextIdAsync("Student", request.Grade);

            var newStudent = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = user.UserId,
                RegistrationNumber = newStudentId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                SchoolName = request.SchoolName,
                Grade = request.Grade,
                ParentName = request.ParentName ?? "Same as Primary",
                DateOfBirth = request.DateOfBirth,
                IsPrimary = false
            };

            await _studentRepository.AddAsync(newStudent);
            await _studentRepository.SaveChangesAsync();

            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);

            var allStudents = await _studentRepository.GetAllAsync(s => s.UserId == user.UserId);
            var profiles = allStudents.Select(s => new StudentProfileDto
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                Grade = s.Grade,
                IsPrimary = s.IsPrimary
            }).ToList();

            var token = GenerateJwtToken(user, role.Name, newStudent.StudentId);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = newStudent.StudentId,
                Profiles = profiles,
                // ✅ Return new student info
                FirstName = newStudent.FirstName,
                LastName = newStudent.LastName,
                RegistrationNumber = newStudent.RegistrationNumber
            };
        }

        public async Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request)
        {
            SocialLoginUser socialUser;
            if (request.Provider.ToLower() == "google")
            {
                socialUser = await ValidateGoogleAccessTokenAsync(request.IdToken);
            }
            else
            {
                throw new Exception("Invalid provider");
            }

            var user = await _userRepository.GetAsync(u => u.Email == socialUser.Email);
            var roleName = "";
            Guid? currentStudentId = null;
            List<StudentProfileDto> profiles = new();

            // ✅ Initialize info vars
            string firstName = "";
            string lastName = "";
            string regNo = "";

            if (user == null)
            {
                // --- NEW USER FLOW ---
                if (string.IsNullOrEmpty(request.Role)) throw new Exception("You should register first.");

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    string normalizedPhone = "+94" + request.PhoneNumber.Substring(1);
                    if (await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone) != null)
                        throw new Exception("This phone number is already in use.");
                }

                var role = await _roleRepository.GetAsync(r => r.Name == request.Role);
                if (role == null) throw new Exception($"Invalid role '{request.Role}' for new user.");
                roleName = role.Name;

                string customId = await _idGeneratorService.GenerateNextIdAsync(request.Role, request.Grade);
                regNo = customId; // ✅ Set generated ID

                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = socialUser.Email,
                    PasswordHash = "",
                    RoleId = role.RoleId,
                    PhoneNumber = !string.IsNullOrEmpty(request.PhoneNumber) ? ("+94" + request.PhoneNumber.Substring(1)) : null
                };

                await _userRepository.AddAsync(user);

                if (roleName == "Student")
                {
                    var student = new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.UserId,
                        RegistrationNumber = customId,
                        FirstName = request.FirstName ?? socialUser.Name.Split(' ')[0],
                        LastName = request.LastName ?? (socialUser.Name.Contains(' ') ? socialUser.Name.Split(' ')[1] : ""),
                        SchoolName = request.SchoolName,
                        Grade = request.Grade,
                        ParentName = request.ParentName,
                        DateOfBirth = request.DateOfBirth ?? DateTime.UtcNow,
                        IsPrimary = true
                    };
                    currentStudentId = student.StudentId;
                    await _studentRepository.AddAsync(student);

                    profiles.Add(new StudentProfileDto
                    {
                        StudentId = student.StudentId,
                        FirstName = student.FirstName,
                        Grade = student.Grade,
                        IsPrimary = true
                    });

                    firstName = student.FirstName;
                    lastName = student.LastName;
                }
                else if (roleName == "Tutor")
                {
                    var tutor = new Tutor
                    {
                        UserId = user.UserId,
                        RegistrationNumber = customId,
                        FirstName = request.FirstName ?? socialUser.Name,
                        LastName = request.LastName,
                        Bio = request.Bio,
                        BankAccountNumber = request.BankAccountNumber,
                        BankName = request.BankName
                    };
                    await _tutorRepository.AddAsync(tutor);

                    firstName = tutor.FirstName;
                    lastName = tutor.LastName;
                }
                else if (roleName == "Institute")
                {
                    var institute = new Institute
                    {
                        UserId = user.UserId,
                        RegistrationNumber = customId,
                        InstituteName = request.InstituteName ?? socialUser.Name,
                        Address = request.Address,
                        ContactNumber = user.PhoneNumber
                    };
                    await _instituteRepository.AddAsync(institute);

                    firstName = institute.InstituteName;
                }

                await _userRepository.SaveChangesAsync();
            }
            else
            {
                // --- EXISTING USER FLOW ---
                var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
                roleName = role.Name;

                // ✅ Fetch existing details
                if (roleName == "Tutor")
                {
                    var tutor = await _tutorRepository.GetAsync(t => t.UserId == user.UserId);
                    if (tutor != null)
                    {
                        firstName = tutor.FirstName;
                        lastName = tutor.LastName;
                        regNo = tutor.RegistrationNumber;
                    }
                }
                else if (roleName == "Institute")
                {
                    var inst = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                    if (inst != null)
                    {
                        firstName = inst.InstituteName;
                        regNo = inst.RegistrationNumber;
                    }
                }
                else if (roleName == "Student")
                {
                    var students = await _studentRepository.GetAllAsync(s => s.UserId == user.UserId);
                    if (students.Any())
                    {
                        profiles = students.Select(s => new StudentProfileDto
                        {
                            StudentId = s.StudentId,
                            FirstName = s.FirstName,
                            Grade = s.Grade,
                            IsPrimary = s.IsPrimary
                        }).ToList();

                        var activeStudent = students.FirstOrDefault(s => s.IsPrimary) ?? students.FirstOrDefault();
                        if (activeStudent != null)
                        {
                            currentStudentId = activeStudent.StudentId;
                            firstName = activeStudent.FirstName;
                            lastName = activeStudent.LastName;
                            regNo = activeStudent.RegistrationNumber;
                        }
                    }
                }
            }

            var token = GenerateJwtToken(user, roleName, currentStudentId);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = roleName,
                Token = token,
                CurrentStudentId = currentStudentId,
                Profiles = profiles,
                // ✅ Return fetched details
                FirstName = firstName,
                LastName = lastName,
                RegistrationNumber = regNo
            };
        }

        private string GenerateJwtToken(User user, string roleName, Guid? studentId = null)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName)
            };

            if (studentId.HasValue)
            {
                claims.Add(new Claim("StudentId", studentId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            var user = await _userRepository.GetAsync(u => u.Email == email);
            return user != null;
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userRepository.GetAsync(u => u.Email == email);
            if (user == null) throw new Exception("This email address is not registered.");

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
            user.PasswordResetToken = token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await _userRepository.SaveChangesAsync();

            string resetLink = $"http://localhost:5173/reset-password?token={token}";
            _emailService.SendEmail(user.Email, "Reset Password", $"Click here to reset your password: <a href='{resetLink}'>Reset Link</a>");
        }

        public async Task SendOtpAsync(string identifier)
        {
            var user = await _userRepository.GetAsync(u => u.Email == identifier);
            if (user == null) throw new Exception("User not found.");

            string otp = new Random().Next(100000, 999999).ToString();

            user.OtpCode = otp;
            user.OtpExpires = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.SaveChangesAsync();

            string subject = "Tutorz Family Verification Code";
            string body = $"<h3>Your verification code is: <span style='color:blue'>{otp}</span></h3><p>Use this code to verify your sibling account.</p>";

            _emailService.SendEmail(user.Email, subject, body);
        }

        public async Task<VerifyUserResponse> VerifyOtpAsync(VerifyUserRequest request)
        {
            var user = await _userRepository.GetAsync(u => u.Email == request.Identifier);

            if (user == null)
                throw new Exception("User not found.");

            if (user.OtpCode != request.Otp)
                throw new Exception("Invalid OTP code.");

            if (user.OtpExpires < DateTime.UtcNow)
                throw new Exception("OTP code has expired.");

            user.OtpCode = null;
            user.OtpExpires = null;
            await _userRepository.SaveChangesAsync();

            return new VerifyUserResponse
            {
                Success = true,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword.Length < 6 || request.NewPassword.Length > 10)
                throw new Exception("Password must be between 6 and 10 characters.");

            var user = await _userRepository.GetAsync(u => u.PasswordResetToken == request.Token);

            if (user == null || user.ResetTokenExpires < DateTime.UtcNow)
                throw new Exception("Invalid or expired token.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            await _userRepository.SaveChangesAsync();
        }

        private async Task<SocialLoginUser> ValidateGoogleAccessTokenAsync(string accessToken)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}");
                if (!response.IsSuccessStatusCode) throw new Exception("Google token validation failed.");

                var content = await response.Content.ReadAsStringAsync();
                var googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email)) throw new Exception("Could not retrieve email from Google.");

                return new SocialLoginUser { Email = googleUser.Email, Name = googleUser.Name };
            }
            catch (Exception ex)
            {
                throw new Exception($"Google validation error: {ex.Message}");
            }
        }

        private class GoogleUserInfo
        {
            public string Sub { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
        }

        private class SocialLoginUser
        {
            public string Email { get; set; }
            public string Name { get; set; }
        }
    }
}