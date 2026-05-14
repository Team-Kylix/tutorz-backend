using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Tutorz.Application.Services
{
    public class InstituteService : IInstituteService
    {

        private readonly IInstituteRepository _instituteRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInstituteStudentRepository _instituteStudentRepository;
        private readonly IInstituteTutorRepository _instituteTutorRepository;
        private readonly IInstituteJoinRequestRepository _joinRequestRepository;
        private readonly IGenericRepository<Class> _classRepository;
        private readonly IGenericRepository<Enrollment> _enrollmentRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IClassPaymentRepository _classPaymentRepository;
        private readonly IProfilePictureService _profilePictureService;
        private readonly IGenericRepository<City> _cityRepository;
        private readonly IGenericRepository<District> _districtRepository;

        public InstituteService(
            IInstituteRepository instituteRepository,
            IStudentRepository studentRepository,
            ITutorRepository tutorRepository,
            IUserRepository userRepository,
            IInstituteStudentRepository instituteStudentRepository,
            IInstituteTutorRepository instituteTutorRepository,
            IInstituteJoinRequestRepository joinRequestRepository,
            IGenericRepository<Class> classRepository,
            IGenericRepository<Enrollment> enrollmentRepository,
            IAttendanceRepository attendanceRepository,
            IClassPaymentRepository classPaymentRepository,
            IProfilePictureService profilePictureService,
            IGenericRepository<City> cityRepository,
            IGenericRepository<District> districtRepository)
        {
            _instituteRepository = instituteRepository;
            _studentRepository = studentRepository;
            _tutorRepository = tutorRepository;
            _userRepository = userRepository;
            _instituteStudentRepository = instituteStudentRepository;
            _instituteTutorRepository = instituteTutorRepository;
            _joinRequestRepository = joinRequestRepository;
            _classRepository = classRepository;
            _enrollmentRepository = enrollmentRepository;
            _attendanceRepository = attendanceRepository;
            _classPaymentRepository = classPaymentRepository;
            _profilePictureService = profilePictureService;
            _cityRepository = cityRepository;
            _districtRepository = districtRepository;
        }

        public async Task<ServiceResponse<InstituteProfileDto>> GetProfileAsync(Guid instituteId)
        {
            
            var institute = await _instituteRepository.GetAsync(
                expression: i => i.InstituteId == instituteId || i.UserId == instituteId,
                includeProperties: "User"
            );

            if (institute == null)
                return new ServiceResponse<InstituteProfileDto> { Success = false, Message = "Institute not found." };

            var dto = new InstituteProfileDto
            {
                InstituteId = institute.InstituteId,
                RegistrationNumber = institute.RegistrationNumber,
                InstituteName = institute.InstituteName,
                Address = institute.Address,
                ContactNumber = institute.ContactNumber,
                Website = institute.Website,
                Email = (institute.User != null) ? institute.User.Email : "",
                CityId = (institute.User != null) ? institute.User.CityId : null,
                IsSmsEnabled = institute.IsSmsEnabled,
                CommissionPercentage = institute.CommissionPercentage,
                ProfileImageUrlSmall = institute.ProfileImageUrlSmall,
                ProfileImageUrlLarge = institute.ProfileImageUrlLarge
            };

            if (dto.CityId.HasValue)
            {
                var city = await _cityRepository.GetAsync(c => c.Id == dto.CityId.Value);
                if (city != null)
                {
                    dto.DistrictId = city.DistrictId;
                    var district = await _districtRepository.GetAsync(d => d.Id == city.DistrictId);
                    if (district != null)
                    {
                        dto.ProvinceId = district.ProvinceId;
                    }
                }
            }

            return new ServiceResponse<InstituteProfileDto> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<InstituteProfileDto>> UpdateProfileAsync(Guid id, UpdateInstituteProfileDto dto)
        {
            
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == id || i.UserId == id);

            if (institute == null)
                return new ServiceResponse<InstituteProfileDto> { Success = false, Message = "Institute not found." };

       
            institute.InstituteName = dto.InstituteName;
            institute.Address = dto.Address;
            institute.ContactNumber = dto.ContactNumber;
            institute.Website = dto.Website;
            institute.IsSmsEnabled = dto.IsSmsEnabled;
            institute.UpdatedDate = DateTime.UtcNow;

            if (dto.ProfilePicture != null)
            {
                try
                {
                    var (smallUrl, largeUrl) = await _profilePictureService.UploadProfilePictureAsync(
                        institute.InstituteId,
                        institute.RegistrationNumber,
                        "Institute",
                        dto.ProfilePicture
                    );

                    institute.ProfileImageUrlSmall = smallUrl;
                    institute.ProfileImageUrlLarge = largeUrl;
                }
                catch (Exception ex)
                {
                    return new ServiceResponse<InstituteProfileDto> 
                    { 
                        Success = false, 
                        Message = $"Failed to upload profile picture: {ex.Message}" 
                    };
                }
            }

            await _instituteRepository.SaveChangesAsync();

            // Update user City location if provided
            if (dto.CityId.HasValue && institute.UserId != Guid.Empty)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == institute.UserId);
                if (user != null)
                {
                    user.CityId = dto.CityId;
                    await _userRepository.SaveChangesAsync();
                }
            }

            return await GetProfileAsync(institute.InstituteId);
        }

        public async Task<ServiceResponse<bool>> AssignStudentAsync(Guid instituteId, AssignStudentDto dto)
        {
            var exists = await _instituteStudentRepository.GetAsync(is_ => is_.InstituteId == instituteId && is_.StudentId == dto.StudentId);
            if (exists != null)
                return new ServiceResponse<bool> { Success = false, Message = "Student is already assigned to this institute." };

            await _instituteStudentRepository.AddAsync(new InstituteStudent
            {
                InstituteId = instituteId,
                StudentId = dto.StudentId,
                AssignedDate = DateTime.UtcNow
            });
            await _instituteStudentRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Student assigned successfully." };
        }

        public async Task<ServiceResponse<bool>> SendTutorRequestAsync(Guid instituteId, AssignTutorDto dto)
        {
            var exists = await _instituteTutorRepository.GetAsync(it => it.InstituteId == instituteId && it.TutorId == dto.TutorId);
            if (exists != null)
                return new ServiceResponse<bool> { Success = false, Message = "Tutor is already assigned to this institute." };

            var pendingRequest = await _joinRequestRepository.GetAsync(r => r.InstituteId == instituteId && r.TutorId == dto.TutorId && r.Status == AssignmentStatus.Pending);
            if (pendingRequest != null)
                return new ServiceResponse<bool> { Success = false, Message = "A pending request already exists for this tutor." };

            await _joinRequestRepository.AddAsync(new InstituteJoinRequest
            {
                InstituteId = instituteId,
                TutorId = dto.TutorId,
                InitiatedBy = RequestInitiator.Institute,
                Status = AssignmentStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });
            await _joinRequestRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Join request sent successfully." };
        }

        public async Task<ServiceResponse<IEnumerable<JoinRequestDto>>> GetIncomingRequestsAsync(Guid instituteId)
        {
            var requests = await _joinRequestRepository.GetAllAsync(
                r => r.InstituteId == instituteId && r.Status == AssignmentStatus.Pending && r.InitiatedBy == RequestInitiator.User && r.TutorId != null,
                includeProperties: "Tutor,Tutor.User"
            );

            var dtos = requests.Select(r => new JoinRequestDto
            {
                RequestId = r.Id,
                InstituteId = r.InstituteId,
                TutorId = r.TutorId,
                TutorName = r.Tutor != null ? $"{r.Tutor.FirstName} {r.Tutor.LastName}" : null,
                TutorRegNumber = r.Tutor != null ? r.Tutor.RegistrationNumber : null,
                TutorPhoneNumber = r.Tutor != null && r.Tutor.User != null ? r.Tutor.User.PhoneNumber : null,
                Status = r.Status.ToString(),
                InitiatedBy = r.InitiatedBy.ToString(),
                RequestedAt = r.RequestedAt
            });

            return new ServiceResponse<IEnumerable<JoinRequestDto>> { Success = true, Data = dtos };
        }

        public async Task<ServiceResponse<bool>> ProcessJoinRequestAsync(Guid instituteId, Guid requestId, string action)
        {
            var request = await _joinRequestRepository.GetAsync(r => r.Id == requestId && r.InstituteId == instituteId);
            if (request == null)
                return new ServiceResponse<bool> { Success = false, Message = "Request not found." };

            if (request.Status != AssignmentStatus.Pending)
                return new ServiceResponse<bool> { Success = false, Message = "Request is already processed." };

            if (action.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                request.Status = AssignmentStatus.Active;
                request.ProcessedAt = DateTime.UtcNow;

                if (request.TutorId.HasValue)
                {
                    await _instituteTutorRepository.AddAsync(new InstituteTutor { InstituteId = instituteId, TutorId = request.TutorId.Value, AssignedDate = DateTime.UtcNow });
                }
            }
            else if (action.Equals("Decline", StringComparison.OrdinalIgnoreCase))
            {
                request.Status = AssignmentStatus.Declined;
                request.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                return new ServiceResponse<bool> { Success = false, Message = "Invalid action." };
            }

            await _joinRequestRepository.SaveChangesAsync();
            await _instituteStudentRepository.SaveChangesAsync();
            await _instituteTutorRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = $"Request {action.ToLower()}ed successfully." };
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid instituteId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower().Trim();

            // Normalize phone for exact matching (converts 07... to +947...)
            string cleanPhone = query.Replace(" ", "").Replace("-", "");
            string exactPhone = cleanPhone;
            if (cleanPhone.StartsWith("0")) exactPhone = "+94" + cleanPhone.Substring(1);
            else if (!cleanPhone.StartsWith("+") && cleanPhone.Length == 9) exactPhone = "+94" + cleanPhone;

            // Get Assigned Student IDs for this institute
            var assignedStudents = await _instituteStudentRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var assignedStudentIds = assignedStudents.Select(a => a.StudentId).ToHashSet();

            var allStudents = await _studentRepository.GetAllAsync();
            var allUsers = await _userRepository.GetAllAsync();
            var userDict = allUsers.ToDictionary(u => u.UserId);

            var results = new List<SearchUserResultDto>();

            foreach (var student in allStudents)
            {
                userDict.TryGetValue(student.UserId, out var user);
                string phone = user?.PhoneNumber ?? "";
                string email = user?.Email ?? "";

                bool isAssigned = assignedStudentIds.Contains(student.StudentId);
                bool isMatch = false;

                if (isAssigned)
                {
                    // Partial match for students already in the institute
                    if ((student.FirstName != null && student.FirstName.ToLower().Contains(query)) ||
                        (student.LastName != null && student.LastName.ToLower().Contains(query)) ||
                        (student.RegistrationNumber != null && student.RegistrationNumber.ToLower().Contains(query)) ||
                        phone.Contains(query) || phone.Contains(cleanPhone) || phone.Contains(exactPhone) ||
                        email.ToLower().Contains(query))
                    {
                        isMatch = true;
                    }
                }
                else
                {
                    // Exact match ONLY for walk-in/unassigned students
                    if ((student.RegistrationNumber != null && student.RegistrationNumber.ToLower() == query) ||
                        phone == exactPhone || phone == query ||
                        (email != "" && email.ToLower() == query))
                    {
                        isMatch = true;
                    }
                }

                if (isMatch)
                {
                    results.Add(new SearchUserResultDto
                    {
                        UserId = student.UserId,
                        RoleSpecificId = student.StudentId,
                        RegistrationNumber = student.RegistrationNumber,
                        Name = $"{student.FirstName} {student.LastName}",
                        PhoneNumber = phone,
                        Email = email,
                        IsAlreadyAssigned = isAssigned
                    });
                }
            }

            return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid instituteId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower().Trim();

            // Normalize phone for exact matching (converts 07... to +947...)
            string cleanPhone = query.Replace(" ", "").Replace("-", "");
            string exactPhone = cleanPhone;
            if (cleanPhone.StartsWith("0")) exactPhone = "+94" + cleanPhone.Substring(1);
            else if (!cleanPhone.StartsWith("+") && cleanPhone.Length == 9) exactPhone = "+94" + cleanPhone;

            // Get Assigned Tutor IDs for this institute
            var assignedTutors = await _instituteTutorRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var assignedTutorIds = assignedTutors.Select(a => a.TutorId).ToHashSet(); // Only declared ONCE!

            var allTutors = await _tutorRepository.GetAllAsync();
            var allUsers = await _userRepository.GetAllAsync();
            var userDict = allUsers.ToDictionary(u => u.UserId);

            var results = new List<SearchUserResultDto>();

            foreach (var tutor in allTutors)
            {
                userDict.TryGetValue(tutor.UserId, out var user);
                string phone = user?.PhoneNumber ?? "";
                string email = user?.Email ?? "";

                bool isAssigned = assignedTutorIds.Contains(tutor.TutorId);
                bool isMatch = false;

                if (isAssigned)
                {
                    // Partial match for tutors already in the institute
                    if ((tutor.FirstName != null && tutor.FirstName.ToLower().Contains(query)) ||
                        (tutor.LastName != null && tutor.LastName.ToLower().Contains(query)) ||
                        (tutor.RegistrationNumber != null && tutor.RegistrationNumber.ToLower().Contains(query)) ||
                        phone.Contains(query) || phone.Contains(cleanPhone) || phone.Contains(exactPhone) ||
                        email.ToLower().Contains(query))
                    {
                        isMatch = true;
                    }
                }
                else
                {
                    // Exact match ONLY for unassigned tutors
                    if ((tutor.RegistrationNumber != null && tutor.RegistrationNumber.ToLower() == query) ||
                        phone == exactPhone || phone == query ||
                        (email != "" && email.ToLower() == query))
                    {
                        isMatch = true;
                    }
                }

                if (isMatch)
                {
                    results.Add(new SearchUserResultDto
                    {
                        UserId = tutor.UserId,
                        RoleSpecificId = tutor.TutorId,
                        RegistrationNumber = tutor.RegistrationNumber,
                        Name = $"{tutor.FirstName} {tutor.LastName}",
                        PhoneNumber = phone,
                        Email = email,
                        IsAlreadyAssigned = isAssigned
                    });
                }
            }

            return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
        }

        public async Task<ServiceResponse<SearchUserResultDto>> SearchTutorExactAsync(Guid instituteId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "Search query is empty." };

            query = query.ToLower().Trim();

            // Normalize phone for exact matching
            string cleanPhone = query.Replace(" ", "").Replace("-", "");
            string exactPhone = cleanPhone;
            if (cleanPhone.StartsWith("0")) exactPhone = "+94" + cleanPhone.Substring(1);
            else if (!cleanPhone.StartsWith("+") && cleanPhone.Length == 9) exactPhone = "+94" + cleanPhone;

            try
            {
                var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId);
                if (institute == null) return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "Institute not found." };

                var tutors = await _tutorRepository.GetAllAsync(
                    t => t.IsActive && (t.RegistrationNumber.ToLower() == query || (t.User != null && t.User.PhoneNumber == exactPhone)),
                    includeProperties: "User,User.City"
                );

                var tutor = tutors.FirstOrDefault();

                if (tutor == null)
                    return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "No active tutor found with the exact Reg No or Mobile Number." };

                var existingAssignment = await _instituteTutorRepository.GetAsync(
                    it => it.InstituteId == institute.InstituteId && it.TutorId == tutor.TutorId);

                var pendingRequest = await _joinRequestRepository.GetAsync(
                    r => r.InstituteId == institute.InstituteId && r.TutorId == tutor.TutorId && r.Status == AssignmentStatus.Pending);

                var result = new SearchUserResultDto
                {
                    UserId = tutor.UserId,
                    RoleSpecificId = tutor.TutorId,
                    RegistrationNumber = tutor.RegistrationNumber,
                    Name = $"{tutor.FirstName} {tutor.LastName}",
                    PhoneNumber = tutor.User?.PhoneNumber,
                    Email = tutor.User?.Email,
                    ProfileImageUrlSmall = tutor.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = tutor.ProfileImageUrlLarge,
                    IsAlreadyAssigned = existingAssignment != null || pendingRequest != null
                };

                return new ServiceResponse<SearchUserResultDto> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "Error searching tutor: " + ex.Message };
            }
        }

        public async Task<ServiceResponse<PaginatedResultDto<StudentProfileDto>>> GetAssignedStudentsAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10)
        {
            var assigned = await _instituteStudentRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var studentIds = assigned.Select(a => a.StudentId).ToList();

            var query = await _studentRepository.GetAllAsync();
            var instituteStudents = query.Where(s => studentIds.Contains(s.StudentId));

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();
                instituteStudents = instituteStudents.Where(s => 
                    s.FirstName.ToLower().Contains(lowerQuery) || 
                    s.LastName.ToLower().Contains(lowerQuery) ||
                    s.RegistrationNumber.ToLower().Contains(lowerQuery)
                );
            }

            var totalCount = instituteStudents.Count();
            var pagedStudents = instituteStudents
                .OrderBy(s => s.FirstName) // Predictable ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var profiles = new List<StudentProfileDto>();
            foreach (var student in pagedStudents)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == student.UserId);
                profiles.Add(new StudentProfileDto
                {
                    StudentId = student.StudentId,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Grade = student.Grade,
                    IsPrimary = student.IsPrimary,
                    RegistrationNumber = student.RegistrationNumber,
                    Email = user?.Email,
                    PhoneNumber = user?.PhoneNumber,
                    ProfileImageUrlSmall = student.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = student.ProfileImageUrlLarge
                });
            }

            var result = new PaginatedResultDto<StudentProfileDto>
            {
                Items = profiles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new ServiceResponse<PaginatedResultDto<StudentProfileDto>> { Success = true, Data = result };
        }

        public async Task<ServiceResponse<PaginatedResultDto<TutorProfileDto>>> GetAssignedTutorsAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10)
        {
            var assigned = await _instituteTutorRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var tutorIds = assigned.Select(a => a.TutorId).ToList();

            var query = await _tutorRepository.GetAllAsync();
            var instituteTutors = query.Where(t => tutorIds.Contains(t.TutorId));

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();
                instituteTutors = instituteTutors.Where(t => 
                    t.FirstName.ToLower().Contains(lowerQuery) || 
                    t.LastName.ToLower().Contains(lowerQuery) ||
                    t.RegistrationNumber.ToLower().Contains(lowerQuery)
                );
            }

            var totalCount = instituteTutors.Count();
            var pagedTutors = instituteTutors
                .OrderBy(t => t.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var profiles = new List<TutorProfileDto>();
            foreach (var tutor in pagedTutors)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == tutor.UserId);
                profiles.Add(new TutorProfileDto
                {
                    TutorId = tutor.TutorId,
                    FirstName = tutor.FirstName,
                    LastName = tutor.LastName,
                    Bio = tutor.Bio,
                    ExperienceYears = tutor.ExperienceYears,
                    Email = user?.Email,
                    PhoneNumber = user?.PhoneNumber,
                    ProfileImageUrlSmall = tutor.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = tutor.ProfileImageUrlLarge,
                    RegistrationNumber = tutor.RegistrationNumber
                });
            }

            var result = new PaginatedResultDto<TutorProfileDto>
            {
                Items = profiles,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return new ServiceResponse<PaginatedResultDto<TutorProfileDto>> { Success = true, Data = result };
        }
        public async Task<ServiceResponse<PaginatedResultDto<InstituteClassDto>>> GetInstituteClassesAsync(Guid instituteId, string searchQuery = "", Guid? tutorId = null, int page = 1, int pageSize = 10)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<PaginatedResultDto<InstituteClassDto>> { Success = false, Message = "Institute not found." };

            var classesQuery = await _classRepository.GetAllAsync(c => c.InstituteId == institute.InstituteId && !c.IsDeleted, includeProperties: "Tutor");
            var classes = classesQuery.AsEnumerable();

            if (tutorId.HasValue)
            {
                classes = classes.Where(c => c.TutorId == tutorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                classes = classes.Where(c => 
                    (c.ClassName != null && c.ClassName.ToLower().Contains(searchQuery)) ||
                    (c.Subject != null && c.Subject.ToLower().Contains(searchQuery)) ||
                    (c.Tutor != null && ((c.Tutor.FirstName != null && c.Tutor.FirstName.ToLower().Contains(searchQuery)) || (c.Tutor.LastName != null && c.Tutor.LastName.ToLower().Contains(searchQuery))))
                );
            }

            classes = classes.ToList();
            int totalItems = classes.Count();
            var pagedClasses = classes.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var dtos = new List<InstituteClassDto>();
            foreach (var c in pagedClasses)
            {
                var enrollments = await _enrollmentRepository.GetAllAsync(e => e.ClassId == c.ClassId && e.Status == EnrollmentStatus.Approved);
                
                dtos.Add(new InstituteClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    ClassType = c.ClassType,
                    Grade = c.Grade,
                    IsActive = c.IsActive,
                    TutorId = c.TutorId,
                    TutorName = c.Tutor != null ? $"{c.Tutor.FirstName} {c.Tutor.LastName}" : "Unknown",
                    Subject = c.Subject,
                    DayOfWeek = c.DayOfWeek,
                    Date = c.Date,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    HallName = c.HallName,
                    Fee = c.Fee,
                    InstituteCommissionRate = c.InstituteCommissionRate,
                    StudentRegisteredCount = enrollments.Count(),
                    StudentCount = enrollments.Count()
                });
            }

            var result = new PaginatedResultDto<InstituteClassDto>
            {
                TotalCount = totalItems,
                Page = page,
                PageSize = pageSize,
                Items = dtos
            };

            return new ServiceResponse<PaginatedResultDto<InstituteClassDto>> { Success = true, Data = result };
        }

        public async Task<ServiceResponse<InstituteClassDto>> CreateInstituteClassAsync(Guid instituteId, CreateClassRequest request)
        {
            if (!request.TutorId.HasValue)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "Tutor must be assigned to create a class from Institute." };

            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "Institute not found." };

            var tutor = await _tutorRepository.GetAsync(t => t.TutorId == request.TutorId.Value);
            if (tutor == null)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "Assigned tutor not found." };

            var assignment = await _instituteTutorRepository.GetAsync(it => it.InstituteId == institute.InstituteId && it.TutorId == tutor.TutorId);
            if (assignment == null)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "The selected tutor is not assigned to your institute." };

            var existingClasses = await _classRepository.GetAllAsync(c => c.TutorId == tutor.TutorId && c.IsActive);

            int newStart = int.Parse(request.StartTime.Replace(":", ""));
            int newEnd = int.Parse(request.EndTime.Replace(":", ""));

            string checkDay = request.DayOfWeek;
            DateTime? checkDate = request.Date;

            if (request.ClassType != "Class" && request.Date.HasValue)
            {
                checkDay = request.Date.Value.DayOfWeek.ToString();
            }

            foreach (var existing in existingClasses)
            {
                bool isDayConflict = false;

                if (request.ClassType == "Class")
                {
                    if (existing.ClassType == "Class" && existing.DayOfWeek == request.DayOfWeek)
                        isDayConflict = true;
                }
                else
                {
                    if (existing.ClassType != "Class" && existing.Date.HasValue && checkDate.HasValue && existing.Date.Value.Date == checkDate.Value.Date)
                        isDayConflict = true;

                    if (existing.ClassType == "Class" && existing.DayOfWeek == checkDay)
                        isDayConflict = true;
                }

                if (isDayConflict)
                {
                    int exStart = int.Parse(existing.StartTime.Replace(":", ""));
                    int exEnd = int.Parse(existing.EndTime.Replace(":", ""));

                    if (newStart < exEnd && newEnd > exStart)
                    {
                        return new ServiceResponse<InstituteClassDto> { Success = false, Message = $"Time Crash! This time overlaps with the Tutor's '{existing.ClassType}': {existing.Subject} ({existing.StartTime} - {existing.EndTime})." };
                    }
                }
            }

            // ── Hall Conflict Check (same institute, same hall, overlapping time) ──
            if (!string.IsNullOrWhiteSpace(request.HallName))
            {
                var hallClasses = await _classRepository.GetAllAsync(
                    c => c.InstituteId == institute.InstituteId && c.IsActive &&
                         c.HallName != null &&
                         c.HallName.ToLower() == request.HallName.ToLower(),
                    includeProperties: "Tutor");

                string newHallCheckDay = request.DayOfWeek;
                if (request.ClassType != "Class" && request.Date.HasValue)
                    newHallCheckDay = request.Date.Value.DayOfWeek.ToString();

                foreach (var hc in hallClasses)
                {
                    bool isHallDayMatch = false;

                    string hcDay = hc.ClassType == "Class"
                        ? hc.DayOfWeek
                        : hc.Date.HasValue ? hc.Date.Value.DayOfWeek.ToString() : null;

                    if (request.ClassType == "Class")
                    {
                        if (hcDay != null && hcDay.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase))
                            isHallDayMatch = true;
                    }
                    else if (request.Date.HasValue)
                    {
                        if (hcDay != null && hcDay.Equals(newHallCheckDay, StringComparison.OrdinalIgnoreCase))
                            isHallDayMatch = true;
                    }

                    if (isHallDayMatch)
                    {
                        int hcStart = int.Parse(hc.StartTime.Replace(":", ""));
                        int hcEnd = int.Parse(hc.EndTime.Replace(":", ""));

                        if (newStart < hcEnd && newEnd > hcStart)
                        {
                            string occupyingTutor = hc.Tutor != null ? $"{hc.Tutor.FirstName} {hc.Tutor.LastName}" : "Another tutor";
                            return new ServiceResponse<InstituteClassDto>
                            {
                                Success = false,
                                Message = $"Cannot create class — {occupyingTutor}'s class already occupies {request.HallName} from {hc.StartTime} to {hc.EndTime} on this day."
                            };
                        }
                    }
                }
            }

            var newClass = new Class
            {
                ClassId = Guid.NewGuid(),
                TutorId = tutor.TutorId,
                InstituteId = institute.InstituteId,
                ClassType = request.ClassType,
                Subject = request.Subject,
                Grade = request.Grade,
                ClassName = !string.IsNullOrEmpty(request.ClassName) ? request.ClassName : $"{request.Subject} ({request.ClassType})",
                DayOfWeek = string.IsNullOrEmpty(request.DayOfWeek) ? "Monday" : request.DayOfWeek,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                HallName = request.HallName,
                Fee = request.Fee,
                IsActive = request.IsActive,
                // Use the caller-supplied rate if provided; otherwise seed from the institute default.
                // This snapshot is later used as the fallback in RecordPaymentAsync.
                InstituteCommissionRate = request.InstituteCommissionRate.HasValue
                    ? request.InstituteCommissionRate.Value
                    : institute.CommissionPercentage,
                CreatedDate = DateTime.UtcNow
            };

            await _classRepository.AddAsync(newClass);
            await _classRepository.SaveChangesAsync();

            var dto = new InstituteClassDto
            {
                ClassId = newClass.ClassId,
                ClassName = newClass.ClassName,
                ClassType = newClass.ClassType,
                TutorName = $"{tutor.FirstName} {tutor.LastName}",
                Subject = newClass.Subject,
                DayOfWeek = newClass.DayOfWeek,
                Date = newClass.Date,
                StartTime = newClass.StartTime,
                EndTime = newClass.EndTime,
                HallName = newClass.HallName,
                Fee = newClass.Fee,
                InstituteCommissionRate = newClass.InstituteCommissionRate,
                StudentRegisteredCount = 0,
                StudentCount = 0
            };

            return new ServiceResponse<InstituteClassDto> { Success = true, Data = dto, Message = "Class created successfully." };
        }

        public async Task<ServiceResponse<InstituteClassDto>> UpdateInstituteClassAsync(Guid instituteId, Guid classId, CreateClassRequest request)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "Institute not found." };

            var existingClass = await _classRepository.GetAsync(c => c.ClassId == classId && c.InstituteId == institute.InstituteId && !c.IsDeleted, includeProperties: "Tutor");
            if (existingClass == null)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "Class not found or access denied." };

            // Allow the frontend to optionally pass TutorId - though not strictly necessary since the class already holds it
            var tutorId = request.TutorId.HasValue ? request.TutorId.Value : existingClass.TutorId;

            existingClass.TutorId = tutorId;
            existingClass.ClassType = request.ClassType;
            existingClass.Subject = request.Subject;
            existingClass.Grade = request.Grade;
            existingClass.ClassName = request.ClassName;
            existingClass.DayOfWeek = string.IsNullOrEmpty(request.DayOfWeek) ? "Monday" : request.DayOfWeek;
            existingClass.Date = request.Date;
            existingClass.StartTime = request.StartTime;
            existingClass.EndTime = request.EndTime;
            existingClass.HallName = request.HallName;
            existingClass.Fee = request.Fee;
            // If the caller provides an override, use it; otherwise keep the existing rate unchanged.
            if (request.InstituteCommissionRate.HasValue)
                existingClass.InstituteCommissionRate = request.InstituteCommissionRate.Value;
            existingClass.UpdatedDate = DateTime.UtcNow;

            // ── Hall Conflict Check on Update (exclude self, same institute + hall + overlapping time) ──
            if (!string.IsNullOrWhiteSpace(request.HallName))
            {
                int newStart = int.Parse(request.StartTime.Replace(":", ""));
                int newEnd   = int.Parse(request.EndTime.Replace(":", ""));

                string newHallCheckDay = request.DayOfWeek;
                if (request.ClassType != "Class" && request.Date.HasValue)
                    newHallCheckDay = request.Date.Value.DayOfWeek.ToString();

                var hallClasses = await _classRepository.GetAllAsync(
                    c => c.InstituteId == institute.InstituteId &&
                         c.IsActive &&
                         c.ClassId != classId &&              // exclude self
                         c.HallName != null &&
                         c.HallName.ToLower() == request.HallName.ToLower(),
                    includeProperties: "Tutor");

                foreach (var hc in hallClasses)
                {
                    string hcDay = hc.ClassType == "Class"
                        ? hc.DayOfWeek
                        : hc.Date.HasValue ? hc.Date.Value.DayOfWeek.ToString() : null;

                    if (hcDay == null) continue;

                    bool dayMatch = false;
                    if (request.ClassType == "Class")
                        dayMatch = hcDay.Equals(request.DayOfWeek, StringComparison.OrdinalIgnoreCase);
                    else if (request.Date.HasValue)
                        dayMatch = hcDay.Equals(newHallCheckDay, StringComparison.OrdinalIgnoreCase);

                    if (!dayMatch) continue;

                    int hcStart = int.Parse(hc.StartTime.Replace(":", ""));
                    int hcEnd   = int.Parse(hc.EndTime.Replace(":", ""));

                    if (newStart < hcEnd && newEnd > hcStart)
                    {
                        string occupyingTutor = hc.Tutor != null ? $"{hc.Tutor.FirstName} {hc.Tutor.LastName}" : "Another tutor";
                        return new ServiceResponse<InstituteClassDto>
                        {
                            Success = false,
                            Message = $"Cannot update class — {occupyingTutor}'s class already occupies {request.HallName} from {hc.StartTime} to {hc.EndTime} on this day."
                        };
                    }
                }
            }

            await _classRepository.SaveChangesAsync();


            // Refresh the tutor reference if it changed
            if (request.TutorId.HasValue && existingClass.TutorId != existingClass.Tutor?.TutorId)
            {
                existingClass = await _classRepository.GetAsync(c => c.ClassId == classId, includeProperties: "Tutor");
            }

            var enrollments = await _enrollmentRepository.GetAllAsync(e => e.ClassId == existingClass.ClassId && e.Status == EnrollmentStatus.Approved);

            var dto = new InstituteClassDto
            {
                ClassId = existingClass.ClassId,
                ClassName = existingClass.ClassName,
                ClassType = existingClass.ClassType,
                Grade = existingClass.Grade,
                IsActive = existingClass.IsActive,
                TutorId = existingClass.TutorId,
                TutorName = existingClass.Tutor != null ? $"{existingClass.Tutor.FirstName} {existingClass.Tutor.LastName}" : "Unknown",
                Subject = existingClass.Subject,
                DayOfWeek = existingClass.DayOfWeek,
                Date = existingClass.Date,
                StartTime = existingClass.StartTime,
                EndTime = existingClass.EndTime,
                HallName = existingClass.HallName,
                Fee = existingClass.Fee,
                InstituteCommissionRate = existingClass.InstituteCommissionRate,
                StudentRegisteredCount = enrollments.Count(),
                StudentCount = enrollments.Count()
            };

            return new ServiceResponse<InstituteClassDto> { Success = true, Data = dto, Message = "Class updated successfully." };
        }

        public async Task<ServiceResponse<bool>> DeleteInstituteClassAsync(Guid instituteId, Guid classId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            var existingClass = await _classRepository.GetAsync(c => c.ClassId == classId && c.InstituteId == institute.InstituteId && !c.IsDeleted);
            if (existingClass == null)
                return new ServiceResponse<bool> { Success = false, Message = "Class not found." };

            // Soft Delete — matches the Hall pattern
            existingClass.IsDeleted = true;
            await _classRepository.SaveChangesAsync();
            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Class deleted successfully." };
        }

        public async Task<ServiceResponse<bool>> ToggleInstituteClassStatusAsync(Guid instituteId, Guid classId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            var existingClass = await _classRepository.GetAsync(
                c => c.ClassId == classId && c.InstituteId == institute.InstituteId && !c.IsDeleted,
                includeProperties: "Tutor");
            if (existingClass == null)
                return new ServiceResponse<bool> { Success = false, Message = "Class not found." };

            // ── If we are REACTIVATING, check for hall conflicts first ──
            if (!existingClass.IsActive && !string.IsNullOrWhiteSpace(existingClass.HallName))
            {
                int newStart = int.Parse(existingClass.StartTime.Replace(":", ""));
                int newEnd   = int.Parse(existingClass.EndTime.Replace(":", ""));

                string newHallCheckDay = existingClass.ClassType == "Class"
                    ? existingClass.DayOfWeek
                    : existingClass.Date.HasValue ? existingClass.Date.Value.DayOfWeek.ToString() : null;

                var hallClasses = await _classRepository.GetAllAsync(
                    c => c.InstituteId == institute.InstituteId &&
                         c.IsActive &&
                         c.ClassId != classId &&
                         c.HallName != null &&
                         c.HallName.ToLower() == existingClass.HallName.ToLower(),
                    includeProperties: "Tutor");

                foreach (var hc in hallClasses)
                {
                    if (newHallCheckDay == null) break;

                    string hcDay = hc.ClassType == "Class"
                        ? hc.DayOfWeek
                        : hc.Date.HasValue ? hc.Date.Value.DayOfWeek.ToString() : null;

                    if (hcDay == null) continue;
                    if (!hcDay.Equals(newHallCheckDay, StringComparison.OrdinalIgnoreCase)) continue;

                    int hcStart = int.Parse(hc.StartTime.Replace(":", ""));
                    int hcEnd   = int.Parse(hc.EndTime.Replace(":", ""));

                    if (newStart < hcEnd && newEnd > hcStart)
                    {
                        string occupyingTutor = hc.Tutor != null ? $"{hc.Tutor.FirstName} {hc.Tutor.LastName}" : "Another tutor";
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = $"Cannot reactivate — {occupyingTutor}'s class already occupies {existingClass.HallName} from {hc.StartTime} to {hc.EndTime} on this day. Deactivate or delete that class first."
                        };
                    }
                }
            }

            existingClass.IsActive = !existingClass.IsActive;
            await _classRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = existingClass.IsActive, Message = "Class status toggled successfully." };
        }


        public async Task<ServiceResponse<IEnumerable<InstituteClassDto>>> GetStudentClassesForAttendanceAsync(Guid instituteId, Guid studentId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<IEnumerable<InstituteClassDto>> { Success = false, Message = "Institute not found." };

            // Find classes for this institute
            var instituteClasses = await _classRepository.GetAllAsync(c => c.InstituteId == institute.InstituteId && c.IsActive && !c.IsDeleted, includeProperties: "Tutor,Enrollments");

            // Find enrollments for this student
            var activeEnrollments = await _enrollmentRepository.GetAllAsync(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Approved);
            var enrolledClassIds = activeEnrollments.Select(e => e.ClassId).ToHashSet();

            var studentEnrolledClasses = instituteClasses.Where(c => enrolledClassIds.Contains(c.ClassId)).ToList();

            var dtos = studentEnrolledClasses.Select(c => new InstituteClassDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                ClassType = c.ClassType,
                Grade = c.Grade,
                IsActive = c.IsActive,
                TutorId = c.TutorId,
                TutorName = c.Tutor != null ? $"{c.Tutor.FirstName} {c.Tutor.LastName}" : "Unknown",
                Subject = c.Subject,
                DayOfWeek = c.DayOfWeek,
                Date = c.Date,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                HallName = c.HallName,
                Fee = c.Fee,
                StudentCount = c.Enrollments?.Count(en => en.Status == EnrollmentStatus.Approved) ?? 0,
                StudentRegisteredCount = c.Enrollments?.Count(en => en.Status == EnrollmentStatus.Approved) ?? 0
            }).ToList();

            return new ServiceResponse<IEnumerable<InstituteClassDto>> { Success = true, Data = dtos };
        }

        public async Task<ServiceResponse<bool>> MarkAttendanceAsync(Guid instituteId, MarkAttendanceDto dto)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            // Auto-assign student to institute if they are not already
            var studentAssignment = await _instituteStudentRepository.GetAsync(is_ => is_.InstituteId == institute.InstituteId && is_.StudentId == dto.StudentId);
            if (studentAssignment == null)
            {
                await _instituteStudentRepository.AddAsync(new InstituteStudent
                {
                    InstituteId = institute.InstituteId,
                    StudentId = dto.StudentId,
                    AssignedDate = DateTime.UtcNow
                });
                await _instituteStudentRepository.SaveChangesAsync();
            }

            // Ensure class belongs to institute
            var instituteClass = await _classRepository.GetAsync(c => c.ClassId == dto.ClassId && c.InstituteId == institute.InstituteId);
            if (instituteClass == null)
                return new ServiceResponse<bool> { Success = false, Message = "Class not found or does not belong to this institute." };

            var attendanceDate = dto.Date ?? DateTime.UtcNow;

            var exists = await _attendanceRepository.HasAttendanceAsync(dto.StudentId, dto.ClassId, institute.InstituteId, attendanceDate);
            if (exists)
                return new ServiceResponse<bool> { Success = false, Message = "Attendance already marked for this date." };

            var attendance = new Attendance
            {
                Id = Guid.NewGuid(),
                StudentId = dto.StudentId,
                ClassId = dto.ClassId,
                InstituteId = institute.InstituteId,
                Date = attendanceDate,
                IsPresent = true,
                MarkedAt = DateTime.UtcNow
            };

            await _attendanceRepository.AddAsync(attendance);
            await _attendanceRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Attendance marked successfully." };
        }

        public async Task<ServiceResponse<IEnumerable<InstituteClassDto>>> GetInstituteClassesTodayAsync(Guid instituteId, DateTime clientDate)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<IEnumerable<InstituteClassDto>> { Success = false, Message = "Institute not found." };

            // Use the client-supplied local date to get the correct DayOfWeek (avoids UTC offset issues)
            var today = clientDate.DayOfWeek.ToString(); // e.g. "Monday"
            var todayDate = clientDate.Date;

            var classes = await _classRepository.GetAllAsync(c => c.InstituteId == institute.InstituteId && c.IsActive && !c.IsDeleted, includeProperties: "Tutor,Enrollments");

            // Filter for today
            var classesToday = classes.Where(c =>
                (c.ClassType == "Class" && c.DayOfWeek == today) ||
                (c.ClassType != "Class" && c.Date.HasValue && c.Date.Value.Date == todayDate)
            ).ToList();

            var dtos = classesToday.Select(c => new InstituteClassDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                ClassType = c.ClassType,
                Grade = c.Grade,
                IsActive = c.IsActive,
                TutorId = c.TutorId,
                TutorName = c.Tutor != null ? $"{c.Tutor.FirstName} {c.Tutor.LastName}" : "Unknown",
                Subject = c.Subject,
                DayOfWeek = c.DayOfWeek,
                Date = c.Date,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                HallName = c.HallName,
                Fee = c.Fee,
                StudentCount = c.Enrollments?.Count(en => en.Status == EnrollmentStatus.Approved) ?? 0,
                StudentRegisteredCount = c.Enrollments?.Count(en => en.Status == EnrollmentStatus.Approved) ?? 0
            }).ToList();

            return new ServiceResponse<IEnumerable<InstituteClassDto>> { Success = true, Data = dtos };
        }

        public async Task<ServiceResponse<bool>> InstantEnrollStudentAsync(Guid instituteId, Guid studentId, Guid classId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            // Auto-assign student to institute if they are not already
            var studentAssignment = await _instituteStudentRepository.GetAsync(is_ => is_.InstituteId == institute.InstituteId && is_.StudentId == studentId);
            if (studentAssignment == null)
            {
                await _instituteStudentRepository.AddAsync(new InstituteStudent
                {
                    InstituteId = institute.InstituteId,
                    StudentId = studentId,
                    AssignedDate = DateTime.UtcNow
                });
                await _instituteStudentRepository.SaveChangesAsync();
            }

            // Ensure class belongs to institute
            var instituteClass = await _classRepository.GetAsync(c => c.ClassId == classId && c.InstituteId == institute.InstituteId);
            if (instituteClass == null)
                return new ServiceResponse<bool> { Success = false, Message = "Class not found or does not belong to this institute." };

            var existingEnrollment = await _enrollmentRepository.GetAsync(e => e.StudentId == studentId && e.ClassId == classId);
            if (existingEnrollment != null)
            {
                if (existingEnrollment.Status == EnrollmentStatus.Approved)
                    return new ServiceResponse<bool> { Success = false, Message = "Student is already enrolled." };
                
                existingEnrollment.Status = EnrollmentStatus.Approved;
                existingEnrollment.EnrolledAt = DateTime.UtcNow;
            }
            else
            {
                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ClassId = classId,
                    Status = EnrollmentStatus.Approved,
                    RequestedAt = DateTime.UtcNow,
                    EnrolledAt = DateTime.UtcNow
                };
                await _enrollmentRepository.AddAsync(enrollment);
            }
            
            await _enrollmentRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Student instantly enrolled successfully." };
        }

        public async Task<ServiceResponse<AttendanceHistoryResponseDto>> GetClassAttendanceHistoryAsync(
            Guid instituteId, Guid? tutorId, Guid? classId, int? year, int? month, string? searchQuery, int page = 1, int pageSize = 10)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<AttendanceHistoryResponseDto> { Success = false, Message = "Institute not found." };

            List<Class> targetClasses = new List<Class>();

            if (classId.HasValue && classId.Value != Guid.Empty)
            {
                var instituteClass = await _classRepository.GetAsync(c => c.ClassId == classId.Value && c.InstituteId == institute.InstituteId);
                if (instituteClass == null)
                    return new ServiceResponse<AttendanceHistoryResponseDto> { Success = false, Message = "Class not found or does not belong to this institute." };
                targetClasses.Add(instituteClass);
            }
            else if (tutorId.HasValue && tutorId.Value != Guid.Empty)
            {
                var classes = await _classRepository.GetAllAsync(c => c.InstituteId == institute.InstituteId && c.TutorId == tutorId.Value);
                if (!classes.Any())
                    return new ServiceResponse<AttendanceHistoryResponseDto> { Success = false, Message = "No classes found for this tutor." };
                targetClasses.AddRange(classes);
            }
            else
            {
                return new ServiceResponse<AttendanceHistoryResponseDto> { Success = false, Message = "Please select a tutor or class." };
            }

            var classIds = targetClasses.Select(c => c.ClassId).ToList();

            // Fetch Attendances for the identified classes
            var attendancesQuery = await _attendanceRepository.GetAllAsync(a => classIds.Contains(a.ClassId) && a.InstituteId == institute.InstituteId);
            
            if (year.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Date.Year == year.Value);
            if (month.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Date.Month == month.Value);
            
            var attendancesList = attendancesQuery.ToList();
            var distinctDates = attendancesList.Select(a => a.Date.Date).Distinct().OrderBy(d => d).ToList();

            // Fetch Enrollments to get students
            var enrollments = await _enrollmentRepository.GetAllAsync(e => classIds.Contains(e.ClassId) && e.Status == EnrollmentStatus.Approved, includeProperties: "Student");
            var distinctStudents = enrollments.Select(e => e.Student).GroupBy(s => s.StudentId).Select(g => g.First()).ToList();

            // Build a lookup: studentId -> set of classIds they are enrolled in
            var studentToClassIds = enrollments
                .GroupBy(e => e.StudentId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ClassId).ToHashSet());

            // Fetch User details for students (for email/phone search)
            var userIds = distinctStudents.Select(s => s.UserId).Distinct().ToList();
            var allUsersQuery = await _userRepository.GetAllAsync();
            var userDict = allUsersQuery.Where(u => userIds.Contains(u.UserId)).ToDictionary(u => u.UserId);

            // Filter Students
            var matchedStudents = new List<Student>();

            foreach (var student in distinctStudents)
            {
                userDict.TryGetValue(student.UserId, out var user);
                string phone = user?.PhoneNumber ?? "";
                string email = user?.Email ?? "";

                bool isMatch = true;

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var lowerQuery = searchQuery.ToLower().Trim();
                    string cleanPhone = lowerQuery.Replace(" ", "").Replace("-", "");
                    
                    isMatch = (student.FirstName != null && student.FirstName.ToLower().Contains(lowerQuery)) ||
                              (student.LastName != null && student.LastName.ToLower().Contains(lowerQuery)) ||
                              (student.RegistrationNumber != null && student.RegistrationNumber.ToLower().Contains(lowerQuery)) ||
                              phone.Contains(lowerQuery) || (!string.IsNullOrEmpty(cleanPhone) && phone.Contains(cleanPhone)) ||
                              email.ToLower().Contains(lowerQuery);
                }

                if (isMatch)
                {
                    matchedStudents.Add(student);
                }
            }

            // Pagination
            int totalCount = matchedStudents.Count;
            var pagedStudents = matchedStudents
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var rowDtos = new List<StudentAttendanceRowDto>();

            foreach (var student in pagedStudents)
            {
                userDict.TryGetValue(student.UserId, out var user);
                string phone = user?.PhoneNumber ?? "";

                var studentAttendances = attendancesList.Where(a => a.StudentId == student.StudentId).ToList();
                var attendanceRecord = new Dictionary<DateTime, bool>();

                foreach (var date in distinctDates)
                {
                    var record = studentAttendances.FirstOrDefault(a => a.Date.Date == date);
                    attendanceRecord[date] = record != null && record.IsPresent;
                }

                // Per-student conducted dates: only dates when THIS student's class(es) ran
                var myClassIds = studentToClassIds.GetValueOrDefault(student.StudentId, new HashSet<Guid>());
                var myClassConductedDates = attendancesList
                    .Where(a => myClassIds.Contains(a.ClassId))
                    .Select(a => a.Date.Date)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                rowDtos.Add(new StudentAttendanceRowDto
                {
                    StudentId = student.StudentId,
                    Name = $"{student.FirstName} {student.LastName}".Trim(),
                    RegistrationNumber = student.RegistrationNumber ?? "",
                    MobileNumber = phone,
                    AttendanceRecord = attendanceRecord,
                    ClassConductedDates = myClassConductedDates
                });
            }

            // --- Summary Statistics Calculation ---
            var matchedStudentIds = matchedStudents.Select(s => s.StudentId).ToList();
            
            // Total Received: Sum of payments for these students in these classes within the period
            decimal totalReceived = await _classPaymentRepository.GetTotalReceivedAsync(classIds, matchedStudentIds, year, month);

            // Total Due: Sum of monthly fees for these students in their respective filtered classes
            // We'll use the enrollments list we already have to sum the fees of 'targetClasses' for 'matchedStudents'
            decimal totalDue = 0;
            if (year.HasValue || month.HasValue)
            {
                // Only calculate 'Due' if we have a specific time period context
                var relevantEnrollments = enrollments.Where(e => matchedStudentIds.Contains(e.StudentId)).ToList();
                foreach (var enrollment in relevantEnrollments)
                {
                    var cls = targetClasses.FirstOrDefault(c => c.ClassId == enrollment.ClassId);
                    if (cls != null) totalDue += cls.Fee;
                }
            }

            var responseDto = new AttendanceHistoryResponseDto
            {
                ConductedDates = distinctDates,
                Students = rowDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalStudentCount = totalCount,
                TotalReceived = totalReceived,
                TotalDue = totalDue
            };

            return new ServiceResponse<AttendanceHistoryResponseDto> { Success = true, Data = responseDto };
        }

        public async Task<ServiceResponse<IEnumerable<InstituteClassDto>>> GetClassesByDateAsync(Guid instituteId, DateTime date)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<IEnumerable<InstituteClassDto>> { Success = false, Message = "Institute not found." };

            string dayOfWeek = date.DayOfWeek.ToString();
            var dateOnly = date.Date;

            var allClasses = await _classRepository.GetAllAsync(
                c => c.InstituteId == institute.InstituteId && c.IsActive && !c.IsDeleted,
                includeProperties: "Tutor,Enrollments");

            // Include recurring classes matching the day-of-week, and one-off classes on that exact date
            var filtered = allClasses.Where(c =>
                (c.ClassType == "Class" && c.DayOfWeek != null &&
                 c.DayOfWeek.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase)) ||
                (c.ClassType != "Class" && c.Date.HasValue && c.Date.Value.Date == dateOnly)
            ).ToList();

            var dtos = filtered.Select(c => new InstituteClassDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                ClassType = c.ClassType,
                Grade = c.Grade,
                IsActive = c.IsActive,
                TutorId = c.TutorId,
                TutorName = c.Tutor != null ? $"{c.Tutor.FirstName} {c.Tutor.LastName}" : "Unknown",
                Subject = c.Subject,
                DayOfWeek = c.DayOfWeek,
                Date = c.Date,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                HallName = c.HallName,
                Fee = c.Fee,
                StudentCount = c.Enrollments?.Count(en => en.Status == EnrollmentStatus.Approved) ?? 0,
                StudentRegisteredCount = c.Enrollments?.Count(en => en.Status == EnrollmentStatus.Approved) ?? 0
            }).ToList();

            return new ServiceResponse<IEnumerable<InstituteClassDto>> { Success = true, Data = dtos };
        }

        public async Task<ServiceResponse<RevenueSummaryDto>> GetRevenueSummaryAsync(Guid instituteId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<RevenueSummaryDto> { Success = false, Message = "Institute not found." };

            // Fetch all active classes for this institute
            var classes = await _classRepository.GetAllAsync(
                c => c.InstituteId == institute.InstituteId && c.IsActive);

            decimal totalGross = 0m;

            foreach (var cls in classes)
            {
                // Count only approved (active) enrollments for this class
                var enrollments = await _enrollmentRepository.GetAllAsync(
                    e => e.ClassId == cls.ClassId && e.Status == EnrollmentStatus.Approved);

                totalGross += cls.Fee * enrollments.Count();
            }

            // Payment tracking not yet implemented — TotalReceived is always 0
            decimal totalReceived = 0m;
            decimal netRevenue = totalGross * (institute.CommissionPercentage / 100m);
            decimal totalDue = totalGross - totalReceived;

            var dto = new RevenueSummaryDto
            {
                TotalGrossRevenue = totalGross,
                InstituteNetRevenue = netRevenue,
                TotalReceived = totalReceived,
                TotalDue = totalDue,
                CommissionPercentage = institute.CommissionPercentage
            };

            return new ServiceResponse<RevenueSummaryDto> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<bool>> UpdateCommissionAsync(Guid instituteId, decimal percentage)
        {
            if (percentage < 0 || percentage > 100)
                return new ServiceResponse<bool> { Success = false, Message = "Commission percentage must be between 0 and 100." };

            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            institute.CommissionPercentage = percentage;
            institute.UpdatedDate = DateTime.UtcNow;

            await _instituteRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Commission percentage updated successfully." };
        }

        public async Task<ServiceResponse<PaginatedResultDto<InstituteProfileDto>>> GetAllInstitutesAsync(string? searchQuery, int page, int pageSize)
        {
            var response = new ServiceResponse<PaginatedResultDto<InstituteProfileDto>>();
            try
            {
                var data = await _instituteRepository.GetAllInstitutesAsync(searchQuery, page, pageSize);
                response.Data = data;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching all institutes: " + ex.Message;
            }
            return response;
        }
    }
}