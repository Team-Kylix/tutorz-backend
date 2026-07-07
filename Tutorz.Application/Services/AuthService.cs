using BCrypt.Net;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tutorz.Application.DTOs.Auth;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

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
        private readonly IQrCodeService _qrCodeService;
        private readonly IInstituteStudentRepository _instituteStudentRepository;
        private readonly IInstituteTutorRepository _instituteTutorRepository;
        private readonly ISmsService _smsService;
        private readonly INotificationService _notificationService;
        private readonly IMemoryCache _cache;
        private readonly IGenericRepository<Admin> _adminRepository;
        private readonly IGenericRepository<Class> _classRepository;

        public AuthService(
            IUserRepository userRepository,
            ITutorRepository tutorRepository,
            IStudentRepository studentRepository,
            IInstituteRepository instituteRepository,
            IRoleRepository roleRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IIdGeneratorService idGeneratorService,
            IQrCodeService qrCodeService,
            IInstituteStudentRepository instituteStudentRepository,
            IInstituteTutorRepository instituteTutorRepository,
            ISmsService smsService,
            INotificationService notificationService,
            IMemoryCache cache,
            IGenericRepository<Admin> adminRepository,
            IGenericRepository<Class> classRepository)
        {
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _tutorRepository = tutorRepository;
            _studentRepository = studentRepository;
            _instituteRepository = instituteRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
            _emailService = emailService;
            _idGeneratorService = idGeneratorService;
            _qrCodeService = qrCodeService;
            _httpClient = new HttpClient();
            _instituteStudentRepository = instituteStudentRepository;
            _instituteTutorRepository = instituteTutorRepository;
            _smsService = smsService;
            _notificationService = notificationService;
            _cache = cache;
            _classRepository = classRepository;
        }

        // --- REGISTER (First Time User) ---
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Password))
            {
                // If registered via an Institute, generate a secure random password.
                // If self-registered (no InstituteId), a 6-digit numeric OTP-style password is fine.
                request.Password = request.InstituteId.HasValue
                    ? GenerateSecurePassword()
                    : new Random().Next(100000, 999999).ToString();
            }
            else if (request.Password.Length < 6 || request.Password.Length > 10)
            {
                throw new Exception("Password must be between 6 and 10 characters.");
            }

            // Phone Number Validation
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                throw new Exception("Phone number is required for registration.");

            if (!Regex.IsMatch(request.PhoneNumber, @"^07\d{8}$"))
                throw new Exception("Invalid phone number format. Must be 10 digits starting with 07 (e.g., 0712345678).");

            string normalizedPhone = "+94" + request.PhoneNumber.Substring(1);
            if (await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone) != null)
                throw new Exception("This phone number is already registered. Please log in.");

            // OTP Verification for Registration (Bypass if registered by an Institute, Tutor, or Class)
            if (!request.InstituteId.HasValue && !request.ClassId.HasValue && !request.TutorId.HasValue)
            {
                if (!_cache.TryGetValue($"registration_otp_{normalizedPhone}", out string cachedOtp) || cachedOtp != request.OtpCode)
                {
                    throw new Exception("Invalid or expired verification code.");
                }
            }

            // Email Validation
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (await _userRepository.GetAsync(u => u.Email == request.Email) != null)
                    throw new Exception("User with this email already exists.");
            }

            var role = await _roleRepository.GetAsync(r => r.Name == request.Role);
            if (role == null) throw new Exception($"Role '{request.Role}' does not exist.");

            // Inherit CityId from the registering Tutor/Institute if not explicitly provided
            if (!request.CityId.HasValue)
            {
                if (request.TutorId.HasValue)
                {
                    var tutorUser = await _userRepository.GetAsync(u => u.UserId == request.TutorId.Value);
                    if (tutorUser?.CityId.HasValue == true)
                        request.CityId = tutorUser.CityId;
                }
                else if (request.InstituteId.HasValue)
                {
                    var instituteUser = await _userRepository.GetAsync(u => u.UserId == request.InstituteId.Value);
                    if (instituteUser?.CityId.HasValue == true)
                        request.CityId = instituteUser.CityId;
                }
            }

            // Generate Unique Registration ID
            string customId = await _idGeneratorService.GenerateNextIdAsync(request.Role, request.Grade);

            // Create User
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = role.RoleId,
                PhoneNumber = normalizedPhone,
                CityId = request.CityId,
                RegistrationNumber = customId,
                IsVerified = true // Verified via OTP before creation
            };
            await _userRepository.AddAsync(user);

            // Handle Specific Roles
            Guid? newStudentId = null;
            Guid? newTutorId = null;

            if (request.Role == "Tutor")
            {
                var tutor = new Tutor
                {
                    TutorId = Guid.NewGuid(),
                    UserId = user.UserId,
                    RegistrationNumber = customId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Bio = request.Bio,
                    BankAccountNumber = request.BankAccountNumber,
                    BankName = request.BankName,
                    ExperienceYears = request.ExperienceYears
                };
                newTutorId = tutor.TutorId;
                await _tutorRepository.AddAsync(tutor);
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
                    IsPrimary = true // First registered student is Primary
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
                    InstituteId = Guid.NewGuid(),
                    UserId = user.UserId,
                    RegistrationNumber = customId, // Assigned to Institute
                    InstituteName = request.InstituteName ?? request.FirstName,
                    Address = request.Address,
                    ContactNumber = normalizedPhone,
                    CommissionPercentage = 25 // Default 25% commission rate for new institutes
                });
            }
            // --- Link to Institute if InstituteId is provided in the request ---
            if (request.InstituteId.HasValue)
            {
                // Verify institute exists
                var institute = await _instituteRepository.GetAsync(i => i.InstituteId == request.InstituteId.Value);
                if (institute == null)
                    throw new Exception("Provided Institute ID does not exist.");

                if (request.Role == "Tutor" && newTutorId.HasValue)
                {
                    await _instituteTutorRepository.AddAsync(new InstituteTutor
                    {
                        InstituteId = request.InstituteId.Value,
                        TutorId = newTutorId.Value,
                        AssignedDate = DateTime.UtcNow
                    });
                }
                else if (request.Role == "Student" && newStudentId.HasValue)
                {
                    await _instituteStudentRepository.AddAsync(new InstituteStudent
                    {
                        InstituteId = request.InstituteId.Value,
                        StudentId = newStudentId.Value,
                        AssignedDate = DateTime.UtcNow
                    });
                }

                // Send Welcome SMS
                if (institute.IsSmsEnabled)
                {
                    try
                    {
                        string frontendUrl = _configuration["FrontendUrl"] ?? "https://www.tutorz.lk";
                        string welcomeMessage = $"Hi {request.FirstName},\nYour user name is {request.PhoneNumber} and password is {request.Password}\nURL to Tutorz: {frontendUrl}";
                        await _smsService.SendSmsAsync(normalizedPhone, welcomeMessage, institute.UserId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Welcome SMS failed for {normalizedPhone}: {ex.Message}");
                    }
                }

                // --- Send notification to Institute ---
                try
                {
                    string notifTitle = request.Role == "Tutor"
                        ? "New Tutor Registered"
                        : "New Student Registered";
                    string notifMessage = $"{request.FirstName} {request.LastName} has been registered under your institute.";
                    string notifType = request.Role == "Tutor" ? "TutorRegistration" : "StudentRegistration";
                    Guid? relatedId = request.Role == "Tutor" ? newTutorId : newStudentId;

                    await _notificationService.CreateAndPushAsync(
                        institute.UserId,
                        notifTitle,
                        notifMessage,
                        notifType,
                        relatedId
                    );
                }
                catch (Exception ex)
                {
                    // Notification failure must never break registration
                    Console.WriteLine($"Notification push failed: {ex.Message}");
                }
            }
            else
            {
                // --- Send Welcome SMS for direct registrations ---
                try
                {
                    string frontendUrl = _configuration["FrontendUrl"] ?? "https://www.tutorz.lk";
                    string welcomeMessage = $"Hi {request.FirstName},\nYour user name is {request.PhoneNumber} and password is {request.Password}\nURL to Tutorz: {frontendUrl}";
                    await _smsService.SendSmsAsync(normalizedPhone, welcomeMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Welcome SMS failed for {normalizedPhone}: {ex.Message}");
                }
            }

            // --- Link to Class if ClassId is provided in the request (Tutor Dashboard Registration) ---
            if (request.ClassId.HasValue && newStudentId.HasValue)
            {
                await _studentRepository.AddEnrollmentAsync(new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = newStudentId.Value,
                    ClassId = request.ClassId.Value,
                    Status = EnrollmentStatus.Approved,
                    EnrolledAt = DateTime.UtcNow
                });

                var cls = await _classRepository.GetAsync(c => c.ClassId == request.ClassId.Value);
                if (cls != null)
                {
                    var tut = await _tutorRepository.GetAsync(t => t.TutorId == cls.TutorId);
                    string tutName = tut != null ? $"{tut.FirstName} {tut.LastName}" : "your tutor";

                    try
                    {
                        await _notificationService.CreateAndPushAsync(
                            user.UserId,
                            "Added to Class",
                            $"You have been added to the class {cls.Subject} {cls.Grade} by {tutName}.",
                            "ClassAdded",
                            cls.ClassId
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ClassAdded notification push failed: {ex.Message}");
                    }
                }
            }

            await _userRepository.SaveChangesAsync();

            // Fetch InstituteId if role is Institute
            Guid? instituteId = null;
            if (request.Role == "Institute")
            {
                 // We need to fetch the Institute created above. 
                 // Since we didn't capture the ID during AddAsync (it returns Task, not entity with ID populated immediately unless EF tracks it... 
                 // actually EF populates the ID on the entity instance after SaveChangesAsync).
                 // However, we created 'new Institute { ... }' inside AddAsync call directly without a variable reference in the implementation above.
                 // Wait, looking at the code: 
                 // await _instituteRepository.AddAsync(new Institute { ... });
                 
                 // We need to fix the Register logic to capture the institute entity first.
                 // Correcting this block requires finding the institute by UserId since we just saved it.
                 var inst = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                 instituteId = inst?.InstituteId;
            }

            // Pass the newStudentId if it exists
            var token = GenerateJwtToken(user, role.Name, newStudentId, instituteId);

            // Determine firstName and lastName based on role
            string firstName = request.Role == "Institute"
                ? (request.InstituteName ?? request.FirstName)
                : request.FirstName;
            string lastName = request.Role == "Institute" ? "" : request.LastName;

            var response = new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = newStudentId,
                FirstName = firstName,
                LastName = lastName,
                RegistrationNumber = customId,
                Profiles = new List<StudentProfileDto>()
            };

            // If student, add self to profiles list
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

        public async Task<ServiceResponse<bool>> CreateAdminAsync(Tutorz.Application.DTOs.System.CreateAdminDto request)
        {
            // Phone Number Validation
            string normalizedPhone = NormalizePhone(request.PhoneNumber);
            if (await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone) != null)
                throw new Exception("This phone number is already registered.");

            // Email Validation
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (await _userRepository.GetAsync(u => u.Email == request.Email) != null)
                    throw new Exception("User with this email already exists.");
            }

            var role = await _roleRepository.GetAsync(r => r.Name == "Admin");
            if (role == null) throw new Exception("Role 'Admin' does not exist.");

            // Generate Unique Registration ID
            string customId = await _idGeneratorService.GenerateNextIdAsync("Admin");

            // Default password: last 6 digits of mobile
            string rawPassword = request.PhoneNumber.Substring(request.PhoneNumber.Length - 6);

            // Create User
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? $"admin.{normalizedPhone.Replace("+", "")}@tutorz.lk" : request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                RoleId = role.RoleId,
                PhoneNumber = normalizedPhone,
                RegistrationNumber = customId,
                IsVerified = true
            };
            
            await _userRepository.AddAsync(user);

            var admin = new Admin
            {
                AdminId = Guid.NewGuid(),
                UserId = user.UserId,
                RegistrationNumber = customId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedDate = DateTime.UtcNow
            };

            await _adminRepository.AddAsync(admin);

            await _userRepository.SaveChangesAsync();

            // Send Welcome SMS
            try
            {
                string welcomeMessage = $"Hi {request.FirstName},\nYou've been added as an Admin. Your user name is {request.PhoneNumber} and password is {rawPassword}\nURL: https://www.tutorz.lk";
                await _smsService.SendSmsAsync(normalizedPhone, welcomeMessage, user.UserId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin Welcome SMS failed: {ex.Message}");
            }

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Admin created successfully." };
        }

        public async Task SendRegistrationOtpAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new Exception("Phone number is required.");

            if (!Regex.IsMatch(phoneNumber, @"^07\d{8}$"))
                throw new Exception("Invalid phone number format. Must be 10 digits starting with 07 (e.g., 0712345678).");

            string normalizedPhone = "+94" + phoneNumber.Substring(1);

            // Check if phone number is already registered to a VERIFIED user
            var existingUser = await _userRepository.GetAsync(u => u.PhoneNumber == normalizedPhone);
            if (existingUser != null && existingUser.IsVerified)
                throw new Exception("This phone number is already registered. Please log in.");

            // Generate 6-digit OTP
            string otp = new Random().Next(100000, 999999).ToString();

            // Store in cache for 10 minutes (Registration Context)
            _cache.Set($"registration_otp_{normalizedPhone}", otp, TimeSpan.FromMinutes(10));

            // Send via SMS
            await _smsService.SendSmsAsync(normalizedPhone, $"Your Tutorz Registration Code is {otp}. Valid for 10 minutes.");
        }

        // --- LOGIN (Handles Multiple Profiles) ---
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
            
            if (!user.IsVerified)
                throw new Exception("Account not verified. Please verify your phone number via OTP first.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("This account was created using Google. Please use the 'Continue with Google' button.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid email/mobile number or password.");

            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
            if (role == null) throw new Exception("User has no valid role.");

            // --- Multi Profile Logic ---
            List<StudentProfileDto> profiles = new();
            Guid? currentStudentId = null;

            if (role.Name == "Student")
            {
                // Fetch ALL students associated with this other User
                var students = await _studentRepository.GetAllAsync(s => s.UserId == user.UserId);

                if (students.Any())
                {
                    profiles = students.Select(s => new StudentProfileDto
                    {
                        StudentId = s.StudentId,
                        FirstName = s.FirstName,
                        Grade = s.Grade,
                        IsPrimary = s.IsPrimary,
                        ProfileImageUrlSmall = s.ProfileImageUrlSmall,
                        ProfileImageUrlLarge = s.ProfileImageUrlLarge
                    }).ToList();

                    // Default to Primary, otherwise first available
                    var activeStudent = students.FirstOrDefault(s => s.IsPrimary) ?? students.FirstOrDefault();
                    currentStudentId = activeStudent?.StudentId;
                }
            }

            // Fetch InstituteId for token if Institute
            Guid? instituteId = null;
            if (role.Name == "Institute")
            {
                var inst = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                instituteId = inst?.InstituteId;
            }

            // Generate token with the selected StudentId and InstituteId
            var token = GenerateJwtToken(user, role.Name, currentStudentId, instituteId);

            // Fetch role-specific data for firstName, lastName, and registrationNumber
            string firstName = null;
            string lastName = null;
            string registrationNumber = null;
            string profileImageUrlSmall = null;
            string profileImageUrlLarge = null;

            if (role.Name == "Tutor")
            {
                var tutor = await _tutorRepository.GetAsync(t => t.UserId == user.UserId);
                if (tutor != null)
                {
                    firstName = tutor.FirstName;
                    lastName = tutor.LastName;
                    registrationNumber = tutor.RegistrationNumber;
                    profileImageUrlSmall = tutor.ProfileImageUrlSmall;
                    profileImageUrlLarge = tutor.ProfileImageUrlLarge;
                }
            }
            else if (role.Name == "Student" && currentStudentId.HasValue)
            {
                var student = await _studentRepository.GetAsync(s => s.StudentId == currentStudentId.Value);
                if (student != null)
                {
                    firstName = student.FirstName;
                    lastName = student.LastName;
                    registrationNumber = student.RegistrationNumber;
                    profileImageUrlSmall = student.ProfileImageUrlSmall;
                    profileImageUrlLarge = student.ProfileImageUrlLarge;
                }
            }
            else if (role.Name == "Institute")
            {
                var institute = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                if (institute != null)
                {
                    firstName = institute.InstituteName;
                    lastName = "";
                    registrationNumber = institute.RegistrationNumber;
                    profileImageUrlSmall = institute.ProfileImageUrlSmall;
                    profileImageUrlLarge = institute.ProfileImageUrlLarge;
                }
            }
            else if (role.Name == "Admin")
            {
                var admin = await _adminRepository.GetAsync(a => a.UserId == user.UserId);
                if (admin != null)
                {
                    firstName = admin.FirstName;
                    lastName = admin.LastName;
                    registrationNumber = admin.RegistrationNumber;
                }
            }

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = currentStudentId,
                FirstName = firstName,
                LastName = lastName,
                RegistrationNumber = registrationNumber,
                ProfileImageUrlSmall = profileImageUrlSmall,
                ProfileImageUrlLarge = profileImageUrlLarge,
                Profiles = profiles
            };
        }

        public async Task<AuthResponse> SwitchProfileAsync(Guid userId, Guid targetStudentId)
        {
            // Verify ownership (Ensure this parent owns this student profile)
            var student = await _studentRepository.GetAsync(s => s.StudentId == targetStudentId && s.UserId == userId);

            if (student == null)
                throw new Exception("Student profile not found or does not belong to this user.");

            // Get User and Role details to regenerate token
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);

            // Generate Token for THIS specific student
            var token = GenerateJwtToken(user, role.Name, student.StudentId, null);

            // Fetch ALL sibling profiles so the switcher remains populated
            var allStudents = await _studentRepository.GetAllAsync(s => s.UserId == userId);
            var profiles = allStudents.Select(s => new StudentProfileDto
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                Grade = s.Grade,
                IsPrimary = s.IsPrimary,
                ProfileImageUrlSmall = s.ProfileImageUrlSmall,
                ProfileImageUrlLarge = s.ProfileImageUrlLarge
            }).ToList();

            // Return response with new token and full sibling list
            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                RegistrationNumber = student.RegistrationNumber,
                ProfileImageUrlSmall = student.ProfileImageUrlSmall,
                ProfileImageUrlLarge = student.ProfileImageUrlLarge,
                Profiles = profiles
            };
        }

        // --- CHECK USER STATUS ---
        public async Task<ServiceResponse<CheckUserResponse>> CheckUserStatusAsync(CheckUserRequest request)
        {
            var response = new ServiceResponse<CheckUserResponse>();
            User user = null;

            // Check Specific Email
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await _userRepository.GetAsync(u => u.Email == request.Email);
            }

            // Check Specific Phone (if user not found yet)
            if (user == null && !string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                string cleanPhone = NormalizePhone(request.PhoneNumber);
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            // Legacy "Identifier" Check (if user still not found)
            if (user == null && !string.IsNullOrWhiteSpace(request.Identifier))
            {
                if (request.Identifier.Contains("@"))
                {
                    user = await _userRepository.GetAsync(u => u.Email == request.Identifier);
                }
                else
                {
                    string cleanPhone = NormalizePhone(request.Identifier);
                    user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
                }
            }

            // --- RESULT PROCESSING (Same as before) ---
            if (user == null)
            {
                response.Success = true;
                response.Data = new CheckUserResponse { Exists = false, Message = "User does not exist." };
                return response;
            }

            // Fetch Details
            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
            string name = "Unknown User";
            Guid? roleSpecificId = null;

            if (role.Name == "Student")
            {
                var student = (await _studentRepository.GetAllAsync(s => s.UserId == user.UserId))
                              .OrderByDescending(s => s.IsPrimary).FirstOrDefault();
                if (student != null)
                {
                    name = $"{student.FirstName} {student.LastName}";
                    roleSpecificId = student.StudentId;
                }
            }
            else if (role.Name == "Tutor")
            {
                var tutor = await _tutorRepository.GetAsync(t => t.UserId == user.UserId);
                if (tutor != null)
                {
                    name = $"{tutor.FirstName} {tutor.LastName}";
                    roleSpecificId = tutor.TutorId;
                }
            }
            else if (role.Name == "Institute")
            {
                var institute = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                if (institute != null)
                {
                    name = institute.InstituteName;
                    roleSpecificId = institute.InstituteId;
                }
            }
            else if (role.Name == "Admin")
            {
                var admin = await _adminRepository.GetAsync(a => a.UserId == user.UserId);
                if (admin != null)
                {
                    name = $"{admin.FirstName} {admin.LastName}";
                    roleSpecificId = admin.AdminId;
                }
            }

            response.Success = true;
            response.Data = new CheckUserResponse
            {
                Exists = true,
                UserId = user.UserId,
                Name = name,
                Role = role.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleSpecificId = roleSpecificId
            };

            return response;
        }

        // Helper method to keep code clean
        private string NormalizePhone(string phone)
        {
            string clean = phone.Replace(" ", "").Replace("-", "");
            if (clean.StartsWith("0")) return "+94" + clean.Substring(1);
            if (!clean.StartsWith("+")) return "+94" + clean;
            return clean;
        }

        // --- REGISTER SIBLING ---
        public async Task<AuthResponse> RegisterSiblingAsync(SiblingRegistrationRequest request)
        {
            // ---Normalize the identifier before searching ---
            string searchIdentifier = request.Identifier;

            // If it's not an email and starts with '0', convert it to '+94' format
            if (!searchIdentifier.Contains("@") && searchIdentifier.StartsWith("0"))
            {
                searchIdentifier = "+94" + searchIdentifier.Substring(1);
            }

            var user = await _userRepository.GetAsync(u => u.Email == searchIdentifier || u.PhoneNumber == searchIdentifier);
            if (user == null) throw new Exception("Parent account not found.");

            if (!user.IsVerified)
                throw new Exception("Parent account is not verified. Please verify the parent account first.");

            // GENERATE NEW ID FOR THE SIBLING
            string newStudentId = await _idGeneratorService.GenerateNextIdAsync("Student", request.Grade);

            // Create the Sibling Student
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

            // Link to Institute if registered via Institute Dashboard
            if (request.InstituteId.HasValue)
            {
                var institute = await _instituteRepository.GetAsync(i => i.InstituteId == request.InstituteId.Value);
                if (institute != null)
                {
                    await _instituteStudentRepository.AddAsync(new InstituteStudent
                    {
                        InstituteId = request.InstituteId.Value,
                        StudentId = newStudent.StudentId,
                        AssignedDate = DateTime.UtcNow
                    });
                    await _instituteStudentRepository.SaveChangesAsync();
                    
                    if (institute.IsSmsEnabled && !string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        try
                        {
                            string frontendUrl = _configuration["FrontendUrl"] ?? "https://www.tutorz.lk";
                            string welcomeMessage = $"Hi,\nA new student profile {newStudentId} for {request.FirstName} has been added to your account by {institute.InstituteName}.\nURL to Tutorz: {frontendUrl}";
                            await _smsService.SendSmsAsync(user.PhoneNumber, welcomeMessage, institute.UserId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Sibling Welcome SMS failed: {ex.Message}");
                        }
                    }

                    // --- Send notification to Institute ---
                    try
                    {
                        await _notificationService.CreateAndPushAsync(
                            institute.UserId,
                            "New Student Registered",
                            $"{request.FirstName} {request.LastName} (sibling) has been registered under your institute.",
                            "StudentRegistration",
                            newStudent.StudentId
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Sibling notification push failed: {ex.Message}");
                    }
                }
            }

            // Auto-Login Response logic
            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);

            // Re fetch all profiles including the new one
            var allStudents = await _studentRepository.GetAllAsync(s => s.UserId == user.UserId);
            var profiles = allStudents.Select(s => new StudentProfileDto
            {
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                Grade = s.Grade,
                IsPrimary = s.IsPrimary
            }).ToList();

            // Generate token for the NEW student specifically
            var token = GenerateJwtToken(user, role.Name, newStudent.StudentId, null);

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = role.Name,
                Token = token,
                CurrentStudentId = newStudent.StudentId,
                FirstName = newStudent.FirstName,
                LastName = newStudent.LastName,
                RegistrationNumber = newStudent.RegistrationNumber,
                Profiles = profiles
            };
        }

        // --- SOCIAL LOGIN ---
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

                // Generate ID
                string customId = await _idGeneratorService.GenerateNextIdAsync(request.Role, request.Grade);

                // Base User
                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = socialUser.Email,
                    PasswordHash = "",
                    RoleId = role.RoleId,
                    PhoneNumber = !string.IsNullOrEmpty(request.PhoneNumber) ? ("+94" + request.PhoneNumber.Substring(1)) : null,
                    CityId = request.CityId,
                    RegistrationNumber = customId,
                    IsVerified = true
                };

                await _userRepository.AddAsync(user);

                // Create Specific Role Entity (Assign customId and LastName here)
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
                }
                else if (roleName == "Tutor")
                {
                    await _tutorRepository.AddAsync(new Tutor
                    {
                        UserId = user.UserId,
                        RegistrationNumber = customId,
                        FirstName = request.FirstName ?? socialUser.Name,
                        LastName = request.LastName,
                        Bio = request.Bio,
                        BankAccountNumber = request.BankAccountNumber,
                        BankName = request.BankName
                    });
                }
                else if (roleName == "Institute")
                {
                    await _instituteRepository.AddAsync(new Institute
                    {
                        InstituteId = Guid.NewGuid(),
                        UserId = user.UserId,
                        RegistrationNumber = customId,
                        InstituteName = request.InstituteName ?? socialUser.Name,
                        Address = request.Address,
                        ContactNumber = user.PhoneNumber,
                        CommissionPercentage = 25 // Default 25% commission rate for new institutes
                    });
                }

                await _userRepository.SaveChangesAsync();
            }
            else
            {
                // --- EXISTING USER FLOW ---
                var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);
                roleName = role.Name;

                if (roleName == "Student")
                {
                    var students = await _studentRepository.GetAllAsync(s => s.UserId == user.UserId);
                    if (students.Any())
                    {
                        profiles = students.Select(s => new StudentProfileDto
                        {
                            StudentId = s.StudentId,
                            FirstName = s.FirstName,
                            Grade = s.Grade,
                            IsPrimary = s.IsPrimary,
                            ProfileImageUrlSmall = s.ProfileImageUrlSmall,
                            ProfileImageUrlLarge = s.ProfileImageUrlLarge
                        }).ToList();

                        var activeStudent = students.FirstOrDefault(s => s.IsPrimary) ?? students.FirstOrDefault();
                        currentStudentId = activeStudent?.StudentId;
                    }
                }
            }

            Guid? instituteId = null;
            if (roleName == "Institute")
            {
                var inst = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                instituteId = inst?.InstituteId;
            }

            var token = GenerateJwtToken(user, roleName, currentStudentId, instituteId);

            // Fetch role-specific data for firstName, lastName, and registrationNumber
            string firstName = null;
            string lastName = null;
            string registrationNumber = null;
            string profileImageUrlSmall = null;
            string profileImageUrlLarge = null;

            if (roleName == "Tutor")
            {
                var tutor = await _tutorRepository.GetAsync(t => t.UserId == user.UserId);
                if (tutor != null)
                {
                    firstName = tutor.FirstName;
                    lastName = tutor.LastName;
                    registrationNumber = tutor.RegistrationNumber;
                    profileImageUrlSmall = tutor.ProfileImageUrlSmall;
                    profileImageUrlLarge = tutor.ProfileImageUrlLarge;
                }
            }
            else if (roleName == "Student" && currentStudentId.HasValue)
            {
                var student = await _studentRepository.GetAsync(s => s.StudentId == currentStudentId.Value);
                if (student != null)
                {
                    firstName = student.FirstName;
                    lastName = student.LastName;
                    registrationNumber = student.RegistrationNumber;
                    profileImageUrlSmall = student.ProfileImageUrlSmall;
                    profileImageUrlLarge = student.ProfileImageUrlLarge;
                }
            }
            else if (roleName == "Institute")
            {
                var institute = await _instituteRepository.GetAsync(i => i.UserId == user.UserId);
                if (institute != null)
                {
                    firstName = institute.InstituteName;
                    lastName = "";
                    registrationNumber = institute.RegistrationNumber;
                    profileImageUrlSmall = institute.ProfileImageUrlSmall;
                    profileImageUrlLarge = institute.ProfileImageUrlLarge;
                }
            }
            else if (roleName == "Admin")
            {
                var admin = await _adminRepository.GetAsync(a => a.UserId == user.UserId);
                if (admin != null)
                {
                    firstName = admin.FirstName;
                    lastName = admin.LastName;
                    registrationNumber = admin.RegistrationNumber;
                }
            }

            return new AuthResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                Role = roleName,
                Token = token,
                CurrentStudentId = currentStudentId,
                FirstName = firstName,
                LastName = lastName,
                RegistrationNumber = registrationNumber,
                ProfileImageUrlSmall = profileImageUrlSmall,
                ProfileImageUrlLarge = profileImageUrlLarge,
                Profiles = profiles
            };
        }
        private string GenerateJwtToken(User user, string roleName, Guid? studentId = null, Guid? instituteId = null)
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

            // ADDED: Include InstituteId claim if role is Institute
            if (instituteId.HasValue)
            {
                claims.Add(new Claim("InstituteId", instituteId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateSecurePassword()
        {
            const string validChars = "1234567890";
            var result = new StringBuilder(6);
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];
                while (result.Length < 6)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    result.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }
            return result.ToString();
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var user = await _userRepository.GetAsync(u => u.Email == email);
            return user != null;
        }

        public async Task ForgotPasswordAsync(string identifier)
        {
            User user = null;
            if (identifier.Contains("@"))
            {
                user = await _userRepository.GetAsync(u => u.Email == identifier);
            }
            else
            {
                string cleanPhone = NormalizePhone(identifier);
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            if (user == null) throw new Exception("This account is not registered.");

            var otp = new Random().Next(100000, 999999).ToString();
            user.PasswordResetToken = otp; // Store 6-digit OTP instead of token
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await _userRepository.SaveChangesAsync();

            string subject = "Reset Password";
            string body = $"<h3>Your password reset code is: <span style='color:blue'>{otp}</span></h3><p>Enter this code on the password reset page.</p>";
            try { _emailService.SendEmail(user.Email, subject, body); } catch { }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                string frontendUrl = _configuration["FrontendUrl"] ?? "https://www.tutorz.lk";
                try { await _smsService.SendSmsAsync(user.PhoneNumber, $"Your Tutorz Password Reset Code is {otp}\nURL to Tutorz: {frontendUrl}"); } catch { }
            }
        }

        public async Task SendOtpAsync(CheckUserRequest request)
        {
            User user = null;

            // Check Email
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                user = await _userRepository.GetAsync(u => u.Email == request.Email);
            }

            // Check Phone
            if (user == null && !string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                string cleanPhone = NormalizePhone(request.PhoneNumber);
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            // Check Identifier
            if (user == null && !string.IsNullOrWhiteSpace(request.Identifier))
            {
                if (request.Identifier.Contains("@"))
                {
                    user = await _userRepository.GetAsync(u => u.Email == request.Identifier);
                }
                else
                {
                    string cleanPhone = NormalizePhone(request.Identifier);
                    user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
                }
            }

            if (user == null) throw new Exception("User not found.");

            // 1. Generate 6-digit Code
            string otp = new Random().Next(100000, 999999).ToString();

            // 2. Save to DB
            user.OtpCode = otp;
            user.OtpExpires = DateTime.UtcNow.AddMinutes(10); // Valid for 10 min
            await _userRepository.SaveChangesAsync();

            // 3. Send Email
            string subject = "Tutorz Family Verification Code";
            string body = $"<h3>Your verification code is: <span style='color:blue'>{otp}</span></h3><p>Use this code to verify your sibling account.</p>";

            try { _emailService.SendEmail(user.Email, subject, body); } catch { }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                string frontendUrl = _configuration["FrontendUrl"] ?? "https://www.tutorz.lk";
                try { await _smsService.SendSmsAsync(user.PhoneNumber, $"Your Tutorz Verification Code is {otp}\nURL to Tutorz: {frontendUrl}"); } catch { }
            }
        }

        // Updated VerifyOtpAsync to return the Phone Number
        public async Task<VerifyUserResponse> VerifyOtpAsync(VerifyUserRequest request)
        {
            User user = null;
            if (request.Identifier.Contains("@"))
            {
                user = await _userRepository.GetAsync(u => u.Email == request.Identifier);
            }
            else
            {
                string cleanPhone = NormalizePhone(request.Identifier);
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            if (user == null)
                throw new Exception("User not found.");

            if (user.OtpCode != request.Otp)
                throw new Exception("Invalid OTP code.");

            if (user.OtpExpires < DateTime.UtcNow)
                throw new Exception("OTP code has expired.");

            // Clear OTP and mark as verified
            user.OtpCode = null;
            user.OtpExpires = null;
            user.IsVerified = true;
            await _userRepository.SaveChangesAsync();

            // Return the phone number stored in the parent account
            return new VerifyUserResponse
            {
                Success = true,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task VerifyResetOtpAsync(VerifyUserRequest request)
        {
            User user = null;
            if (request.Identifier.Contains("@"))
            {
                user = await _userRepository.GetAsync(u => u.Email == request.Identifier);
            }
            else
            {
                string cleanPhone = NormalizePhone(request.Identifier);
                user = await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone);
            }

            if (user == null)
                throw new Exception("User not found.");

            if (user.PasswordResetToken != request.Otp)
                throw new Exception("Invalid OTP code.");

            if (user.ResetTokenExpires < DateTime.UtcNow)
                throw new Exception("OTP code has expired.");
                
            // Do NOT clear the token here, as it will be used in the ResetPassword step.
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

        // --- CREDENTIAL UPDATES ---
        public async Task RequestEmailUpdateAsync(Guid userId, string newEmail)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null) throw new Exception("User not found.");

            if (await _userRepository.GetAsync(u => u.Email == newEmail && u.UserId != userId) != null)
                throw new Exception("This email is already in use by another account.");

            string otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpires = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Generated OTP for email update ({newEmail}): {otp}");

            string subject = "Tutorz Email Update Verification";
            string body = $"<h3>Your verification code is: <span style='color:blue'>{otp}</span></h3><p>Use this code to verify your new email address.</p>";
            try { 
                _emailService.SendEmail(newEmail, subject, body); 
            } 
            catch (Exception ex) { 
                throw new Exception($"Failed to send email. Please check SMTP credentials or connection. Detail: {ex.Message}"); 
            }
        }

        public async Task<ServiceResponse<bool>> VerifyEmailUpdateAsync(Guid userId, VerifyCredentialUpdateDto request)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null) return new ServiceResponse<bool> { Success = false, Message = "User not found." };

            if (user.OtpCode != request.Otp || user.OtpExpires < DateTime.UtcNow)
                return new ServiceResponse<bool> { Success = false, Message = "Invalid or expired OTP." };

            user.Email = request.NewIdentifier;
            user.OtpCode = null;
            user.OtpExpires = null;
            await _userRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Message = "Email updated successfully.", Data = true };
        }

        public async Task RequestMobileUpdateAsync(Guid userId, string newMobile)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null) throw new Exception("User not found.");

            string cleanPhone = NormalizePhone(newMobile);

            if (await _userRepository.GetAsync(u => u.PhoneNumber == cleanPhone && u.UserId != userId) != null)
                throw new Exception("This mobile number is already in use by another account.");

            string otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpires = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] Generated OTP for mobile update ({cleanPhone}): {otp}");

            try { await _smsService.SendSmsAsync(cleanPhone, $"Your Tutorz Verification Code is {otp}", userId); } 
            catch (Exception ex) { 
                throw new Exception($"Failed to send SMS. Please check SMS API configuration. Detail: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> VerifyMobileUpdateAsync(Guid userId, VerifyCredentialUpdateDto request)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null) return new ServiceResponse<bool> { Success = false, Message = "User not found." };

            if (user.OtpCode != request.Otp || user.OtpExpires < DateTime.UtcNow)
                return new ServiceResponse<bool> { Success = false, Message = "Invalid or expired OTP." };

            user.PhoneNumber = NormalizePhone(request.NewIdentifier);
            user.OtpCode = null;
            user.OtpExpires = null;
            await _userRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Message = "Mobile number updated successfully.", Data = true };
        }

        public async Task<ServiceResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto request)
        {
            if (request.NewPassword.Length < 6 || request.NewPassword.Length > 10)
                return new ServiceResponse<bool> { Success = false, Message = "New password must be between 6 and 10 characters." };

            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null) return new ServiceResponse<bool> { Success = false, Message = "User not found." };

            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return new ServiceResponse<bool> { Success = false, Message = "Incorrect current password." };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Message = "Password changed successfully.", Data = true };
        }

        // Private helper for Google
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