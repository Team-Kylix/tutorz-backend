using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Domain.Enums;

namespace Tutorz.Application.Services
{
    public class TutorService : ITutorService
    {
        private readonly ITutorRepository _tutorRepo;
        private readonly IGenericRepository<Class> _classRepo; 
        private readonly IStudentRepository _studentRepo;
        private readonly IGenericRepository<Enrollment> _enrollmentRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IUserRepository _userRepo;
        private readonly IInstituteJoinRequestRepository _joinRequestRepo;
        private readonly IInstituteTutorRepository _instituteTutorRepo;
        private readonly IInstituteRepository _instituteRepo;
        private readonly IProfilePictureService _profilePictureService;
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<District> _districtRepo;
        private readonly INotificationService _notificationService;
        private readonly IGenericRepository<MarkSheet> _markSheetRepo;
        private readonly IGenericRepository<MarkRecord> _markRecordRepo;
        private readonly IGenericRepository<ClassPayment> _paymentRepo;
        private readonly IGenericRepository<Withdrawal> _withdrawalRepo;

        public TutorService(
            ITutorRepository tutorRepo,
            IGenericRepository<Class> classRepo,
            IStudentRepository studentRepo,
            IGenericRepository<Enrollment> enrollmentRepo,
            IAttendanceRepository attendanceRepo,
            IUserRepository userRepo,
            IInstituteJoinRequestRepository joinRequestRepo,
            IInstituteTutorRepository instituteTutorRepo,
            IInstituteRepository instituteRepo,
            IProfilePictureService profilePictureService,
            IGenericRepository<City> cityRepo,
            IGenericRepository<District> districtRepo,
            INotificationService notificationService,
            IGenericRepository<MarkSheet> markSheetRepo,
            IGenericRepository<MarkRecord> markRecordRepo,
            IGenericRepository<ClassPayment> paymentRepo,
            IGenericRepository<Withdrawal> withdrawalRepo)
        {
            _tutorRepo = tutorRepo;
            _classRepo = classRepo;
            _studentRepo = studentRepo;
            _enrollmentRepo = enrollmentRepo;
            _attendanceRepo = attendanceRepo;
            _userRepo = userRepo;
            _joinRequestRepo = joinRequestRepo;
            _instituteTutorRepo = instituteTutorRepo;
            _instituteRepo = instituteRepo;
            _profilePictureService = profilePictureService;
            _cityRepo = cityRepo;
            _districtRepo = districtRepo;
            _notificationService = notificationService;
            _markSheetRepo = markSheetRepo;
            _markRecordRepo = markRecordRepo;
            _paymentRepo = paymentRepo;
            _withdrawalRepo = withdrawalRepo;
        }

        public async Task<ServiceResponse<ClassDto>> CreateClassAsync(Guid userId, CreateClassRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) throw new Exception("Tutor profile not found.");

            var existingClasses = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId && c.IsActive);

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
                        return new ServiceResponse<ClassDto>
                        {
                            Success = false,
                            Message = $"Time Crash! This time overlaps with your '{existing.ClassType}': {existing.Subject} ({existing.StartTime} – {existing.EndTime})."
                        };
                    }
                }
            }

            // ── Hall Conflict Check (same institute, same hall, overlapping time) ──
            if (request.InstituteId.HasValue && !string.IsNullOrWhiteSpace(request.HallName))
            {
                var hallClasses = await _classRepo.GetAllAsync(
                    c => c.InstituteId == request.InstituteId.Value && c.IsActive &&
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
                            return new ServiceResponse<ClassDto>
                            {
                                Success = false,
                                Message = $"Hall Conflict! {occupyingTutor}'s class already occupies {request.HallName} from {hc.StartTime} – {hc.EndTime} on this day."
                            };
                        }
                    }
                }
            }


            decimal instituteCommissionRate = 0;
            if (request.InstituteId.HasValue)
            {
                var inst = await _instituteRepo.GetAsync(i => i.InstituteId == request.InstituteId.Value);
                if (inst != null)
                {
                    instituteCommissionRate = inst.CommissionPercentage;
                }
            }

            var newClass = new Class
            {
                ClassId = Guid.NewGuid(),
                TutorId = tutor.TutorId,
                InstituteId = request.InstituteId,
                ClassType = request.ClassType,
                Subject = request.Subject,
                Grade = request.Grade,
                ClassName = !string.IsNullOrEmpty(request.ClassName) ? request.ClassName : $"{request.Subject} ({request.ClassType})",
                DayOfWeek = request.DayOfWeek,
                Date = request.Date,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                HallName = request.HallName,
                Fee = request.Fee,
                InstituteCommissionRate = instituteCommissionRate,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            await _classRepo.AddAsync(newClass);
            await _classRepo.SaveChangesAsync();

            return new ServiceResponse<ClassDto> { Success = true, Data = MapToDto(newClass), Message = "Class created successfully." };
        }

        public async Task<ServiceResponse<ClassDto>> UpdateClassAsync(Guid classId, Guid userId, CreateClassRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            var existingClass = await _classRepo.GetAsync(c => c.ClassId == classId && c.TutorId == tutor.TutorId, includeProperties: "Enrollments,Institute");

            if (existingClass == null) throw new Exception("Class not found or access denied.");

            existingClass.InstituteId = request.InstituteId;
            existingClass.ClassType = request.ClassType;
            existingClass.Subject = request.Subject;
            existingClass.Grade = request.Grade;
            existingClass.ClassName = request.ClassName;
            existingClass.DayOfWeek = request.DayOfWeek;
            existingClass.Date = request.Date;
            existingClass.StartTime = request.StartTime;
            existingClass.EndTime = request.EndTime;
            existingClass.HallName = request.HallName;
            existingClass.Fee = request.Fee;
            existingClass.IsActive = request.IsActive;
            
            // Tutors cannot edit the commission rate, but we refresh it if the institute changed
            if (request.InstituteId.HasValue)
            {
                var inst = await _instituteRepo.GetAsync(i => i.InstituteId == request.InstituteId.Value);
                if (inst != null)
                {
                    existingClass.InstituteCommissionRate = inst.CommissionPercentage;
                }
            }
            else
            {
                existingClass.InstituteCommissionRate = 0;
            }

            existingClass.UpdatedDate = DateTime.UtcNow;

            // ── Hall Conflict Check on Update (same institute + hall, exclude self) ──
            if (request.InstituteId.HasValue && !string.IsNullOrWhiteSpace(request.HallName))
            {
                int newStart = int.Parse(request.StartTime.Replace(":", ""));
                int newEnd   = int.Parse(request.EndTime.Replace(":", ""));

                string newHallCheckDay = request.DayOfWeek;
                if (request.ClassType != "Class" && request.Date.HasValue)
                    newHallCheckDay = request.Date.Value.DayOfWeek.ToString();

                var hallClasses = await _classRepo.GetAllAsync(
                    c => c.InstituteId == request.InstituteId.Value &&
                         c.IsActive &&
                         c.ClassId != classId &&          // exclude self
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
                        return new ServiceResponse<ClassDto>
                        {
                            Success = false,
                            Message = $"Hall Conflict! {occupyingTutor}'s class already occupies {request.HallName} from {hc.StartTime} – {hc.EndTime} on this day."
                        };
                    }
                }
            }

            await _classRepo.SaveChangesAsync();
            return new ServiceResponse<ClassDto> { Success = true, Data = MapToDto(existingClass), Message = "Class updated successfully." };
        }


        public async Task DeleteClassAsync(Guid classId, Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            var existingClass = await _classRepo.GetAsync(c => c.ClassId == classId && c.TutorId == tutor.TutorId);
            if (existingClass == null) throw new Exception("Class not found.");

            await _classRepo.DeleteAsync(existingClass);
        }

        public async Task<bool> AddStudentToClassAsync(Guid userId, AddStudentRequest request)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            
            var existingClass = await _classRepo.GetAsync(
                c => c.ClassId == request.ClassId && c.TutorId == tutor.TutorId,
                includeProperties: "Institute");

            if (existingClass == null) throw new Exception("Class not found or access denied.");

            if (existingClass.InstituteId.HasValue)
            {
                var instName = existingClass.Institute?.InstituteName ?? "an institute";
                throw new Exception($"This class is held at {instName}. Only the institute can add students directly to the class, or the student should send a request to join the class.");
            }

            // Normalize the input: if the user typed a local mobile (e.g. 0712345678), convert to +94 format
            string lookup = request.StudentRegistrationNumber?.Trim() ?? "";
            string normalizedPhone = null;
            if (System.Text.RegularExpressions.Regex.IsMatch(lookup, @"^07\d{8}$"))
            {
                normalizedPhone = "+94" + lookup.Substring(1);
            }

            var student = await _studentRepo.GetStudentByPhoneOrRegNoAsync(lookup, normalizedPhone);

            if (student == null) throw new Exception("Student not found. Please enter the full Registration Number (e.g. STU-XXXXXX) or Mobile Number (e.g. 07XXXXXXXX).");

            // Check if the student is already in the class
            var enrollments = await _studentRepo.GetEnrollmentsByClassAsync(request.ClassId);
            if (enrollments.Any(e => e.StudentId == student.StudentId))
                throw new Exception("Student is already enrolled in this class.");

            // Add enrollment
            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student.StudentId,
                ClassId = request.ClassId,
                Status = EnrollmentStatus.Approved, // Auto approve since tutor is adding
                EnrolledAt = DateTime.UtcNow
            };

            await _studentRepo.AddEnrollmentAsync(enrollment);
            await _studentRepo.SaveChangesAsync();
            
            // Add Notification
            await _notificationService.CreateAndPushAsync(
                student.UserId,
                "Added to Class",
                $"You have been added to the class {existingClass.ClassName} by tutor {tutor.FirstName} {tutor.LastName}.",
                "ClassAdded",
                existingClass.ClassId
            );

            return true;
        }

        public async Task<List<ClassDto>> GetClassesAsync(Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) throw new Exception("Tutor profile not found.");

            var classes = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId, includeProperties: "Institute,Enrollments");

            return classes.Select(c => MapToDto(c)).ToList();
        }

        public async Task<ServiceResponse<TutorProfileDto>> GetTutorProfileAsync(Guid userId)
        {
            var response = new ServiceResponse<TutorProfileDto>();
            var profileDto = await _tutorRepo.GetTutorProfileAsync(userId);

            if (profileDto == null)
            {
                response.Success = false;
                response.Message = "Profile not found";
                return response;
            }

            // Populate Location names and IDs
            if (profileDto.CityId.HasValue)
            {
                var city = await _cityRepo.GetAsync(c => c.Id == profileDto.CityId.Value);
                if (city != null)
                {
                    profileDto.DistrictId = city.DistrictId;
                    var district = await _districtRepo.GetAsync(d => d.Id == city.DistrictId);
                    if (district != null)
                    {
                        profileDto.ProvinceId = district.ProvinceId;
                    }
                }
            }

            response.Data = profileDto;
            return response;
        }

        public async Task<ServiceResponse<TutorProfileDto>> UpdateTutorProfileAsync(Guid userId, UpdateTutorProfileDto request)
        {
            var response = new ServiceResponse<TutorProfileDto>();

            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null)
            {
                response.Success = false;
                response.Message = "Tutor not found.";
                return response;
            }

            var user = await _userRepo.GetAsync(u => u.UserId == userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                return response;
            }

            // Update Tutor entity
            tutor.FirstName = request.FirstName;
            tutor.LastName = request.LastName;
            tutor.Bio = request.Bio ?? "";
            tutor.Address = request.Address;
            tutor.UpdatedDate = DateTime.UtcNow;

            // Update User entity
            if (request.CityId.HasValue)
            {
                user.CityId = request.CityId;
            }
            user.UpdatedDate = DateTime.UtcNow;

            // Handle Profile Picture
            if (request.ProfilePicture != null)
            {
                try
                {
                    var (smallUrl, largeUrl) = await _profilePictureService.UploadProfilePictureAsync(
                        tutor.TutorId,
                        tutor.RegistrationNumber,
                        "Tutor",
                        request.ProfilePicture
                    );

                    tutor.ProfileImageUrlSmall = smallUrl;
                    tutor.ProfileImageUrlLarge = largeUrl;
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.Message = $"Image upload failed: {ex.Message}";
                    return response;
                }
            }

            await _tutorRepo.SaveChangesAsync();
            await _userRepo.SaveChangesAsync();

            return await GetTutorProfileAsync(userId);
        }

        public async Task<List<StudentRequestDto>> GetStudentRequestsAsync(Guid userId)
        {
            return await _tutorRepo.GetPendingRequestsAsync(userId);
        }

        public async Task<bool> ProcessStudentRequestsAsync(ProcessRequestDto request)
        {
            if (request.EnrollmentIds == null || request.EnrollmentIds.Count == 0)
                return false;

            var enrollments = await _tutorRepo.GetEnrollmentsByIdsAsync(request.EnrollmentIds);

            if (enrollments.Count == 0)
                return false;

            EnrollmentStatus newStatus;
            if (request.Action.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = EnrollmentStatus.Approved;
            }
            else if (request.Action.Equals("Declined", StringComparison.OrdinalIgnoreCase))
            {
                newStatus = EnrollmentStatus.Rejected;
            }
            else
            {
                return false;
            }

            foreach (var enrollment in enrollments)
            {
                if (enrollment.Status == EnrollmentStatus.Pending)
                {
                    enrollment.Status = newStatus;
                    enrollment.EnrolledAt = DateTime.UtcNow;

                    // Notify student
                    if (enrollment.Student != null && enrollment.Student.UserId != Guid.Empty && enrollment.Class != null)
                    {
                        string actionText = request.Action.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ? "accepted" : "declined";
                        string title = $"Class Request {actionText.ToUpper()}";
                        string message = $"Your request to join {enrollment.Class.ClassName} has been {actionText}.";
                        string notificationType = request.Action.Equals("Accepted", StringComparison.OrdinalIgnoreCase) ? "request_approved" : "request_rejected";

                        await _notificationService.CreateAndPushAsync(
                            enrollment.Student.UserId,
                            title,
                            message,
                            notificationType,
                            enrollment.ClassId
                        );
                    }
                }
            }

            await _tutorRepo.UpdateEnrollmentsAsync(enrollments);
            return true;
        }

        public async Task<StudentFullProfileDto> GetStudentProfileAsync(Guid studentId)
        {
            return await _tutorRepo.GetStudentProfileForTutorAsync(studentId);
        }

        public async Task<ServiceResponse<bool>> SendInstituteRequestAsync(Guid userId, Guid instituteId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<bool> { Success = false, Message = "Tutor not found." };
            var tutorId = tutor.TutorId;

            var exists = await _instituteTutorRepo.GetAsync(it => it.InstituteId == instituteId && it.TutorId == tutorId);
            if (exists != null)
                return new ServiceResponse<bool> { Success = false, Message = "Already assigned to this institute." };

            var pendingRequest = await _joinRequestRepo.GetAsync(r => r.InstituteId == instituteId && r.TutorId == tutorId && r.Status == AssignmentStatus.Pending);
            if (pendingRequest != null)
                return new ServiceResponse<bool> { Success = false, Message = "A pending request already exists." };

            await _joinRequestRepo.AddAsync(new InstituteJoinRequest
            {
                InstituteId = instituteId,
                TutorId = tutorId,
                InitiatedBy = RequestInitiator.User,
                Status = AssignmentStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });
            await _joinRequestRepo.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Join request sent successfully." };
        }

        public async Task<ServiceResponse<IEnumerable<JoinRequestDto>>> GetInstituteRequestsAsync(Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<IEnumerable<JoinRequestDto>> { Success = false, Message = "Tutor not found." };
            var tutorId = tutor.TutorId;

            var requests = await _joinRequestRepo.GetAllAsync(
                r => r.TutorId == tutorId && r.Status == AssignmentStatus.Pending && r.InitiatedBy == RequestInitiator.Institute,
                includeProperties: "Institute"
            );

            var dtos = requests.Select(r => new JoinRequestDto
            {
                RequestId = r.Id,
                InstituteId = r.InstituteId,
                InstituteName = r.Institute != null ? r.Institute.InstituteName : null,
                InstituteRegNumber = r.Institute != null ? r.Institute.RegistrationNumber : null,
                InstitutePhoneNumber = r.Institute != null ? r.Institute.ContactNumber : null,
                TutorId = r.TutorId,
                Status = r.Status.ToString(),
                InitiatedBy = r.InitiatedBy.ToString(),
                RequestedAt = r.RequestedAt
            });

            return new ServiceResponse<IEnumerable<JoinRequestDto>> { Success = true, Data = dtos };
        }

        public async Task<ServiceResponse<bool>> ProcessInstituteRequestAsync(Guid userId, Guid requestId, string action)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<bool> { Success = false, Message = "Tutor not found." };
            var tutorId = tutor.TutorId;

            var request = await _joinRequestRepo.GetAsync(r => r.Id == requestId && r.TutorId == tutorId);
            if (request == null)
                return new ServiceResponse<bool> { Success = false, Message = "Request not found." };

            if (request.Status != AssignmentStatus.Pending)
                return new ServiceResponse<bool> { Success = false, Message = "Request is already processed." };

            if (action.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                request.Status = AssignmentStatus.Active;
                request.ProcessedAt = DateTime.UtcNow;

                await _instituteTutorRepo.AddAsync(new InstituteTutor { InstituteId = request.InstituteId, TutorId = tutorId, AssignedDate = DateTime.UtcNow });
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

            await _joinRequestRepo.SaveChangesAsync();
            await _instituteTutorRepo.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true, Message = $"Request {action.ToLower()}ed successfully." };
        }

        public async Task<ServiceResponse<IEnumerable<InstituteDto>>> GetJoinedInstitutesAsync(Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<IEnumerable<InstituteDto>> { Success = false, Message = "Tutor not found." };

            var assignments = await _instituteTutorRepo.GetAllAsync(
                it => it.TutorId == tutor.TutorId,
                includeProperties: "Institute,Institute.User,Institute.User.City"
            );

            var dtos = assignments.Where(a => a.Institute != null).Select(a => new InstituteDto
            {
                InstituteId = a.Institute.InstituteId,
                Name = a.Institute.InstituteName,
                City = a.Institute.User?.City?.Name,
                CommissionPercentage = a.Institute.CommissionPercentage
            });

            return new ServiceResponse<IEnumerable<InstituteDto>> { Success = true, Data = dtos };
        }

        private ClassDto MapToDto(Class entity)
        {
            return new ClassDto
            {
                ClassId = entity.ClassId,
                InstituteId = entity.InstituteId,
                InstituteName = entity.Institute?.InstituteName ?? string.Empty,
                ClassType = entity.ClassType,
                Subject = entity.Subject,
                Grade = entity.Grade,
                ClassName = entity.ClassName,
                DayOfWeek = entity.DayOfWeek,
                Date = entity.Date,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                HallName = entity.HallName,
                Fee = entity.Fee,
                IsActive = entity.IsActive,
                InstituteCommissionRate = entity.InstituteCommissionRate,
                StudentCount = entity.Enrollments?.Count(e => e.Status == EnrollmentStatus.Approved) ?? 0
            };
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid userId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower().Trim();

            try
            {
                var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
                if (tutor == null) return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = false, Message = "Tutor not found." };

                // Get all classes for this tutor
                var classes = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId);
                var classIds = classes.Select(c => c.ClassId).ToList();

                // Get all approved enrollments for these classes
                var results = new List<SearchUserResultDto>();
                var seenStudentIds = new HashSet<Guid>();

                foreach (var classId in classIds)
                {
                    var enrollments = await _studentRepo.GetEnrollmentsByClassAsync(classId);
                    var approvedEnrollments = enrollments.Where(e => e.Status == EnrollmentStatus.Approved);

                    foreach (var enrollment in approvedEnrollments)
                    {
                        if (enrollment.StudentId != Guid.Empty && !seenStudentIds.Contains(enrollment.StudentId))
                        {
                            var student = enrollment.Student ?? await _studentRepo.GetAsync(
                                s => s.StudentId == enrollment.StudentId,
                                includeProperties: "User"
                            );

                            if (student != null)
                            {
                                string fullName = $"{student.FirstName} {student.LastName}".ToLower();
                                string regNo = student.RegistrationNumber?.ToLower() ?? "";
                                string phone = student.User?.PhoneNumber ?? "";
                                string email = student.User?.Email?.ToLower() ?? "";

                                if (fullName.Contains(query) || regNo.Contains(query) || phone.Contains(query) || email.Contains(query))
                                {
                                    results.Add(new SearchUserResultDto
                                    {
                                        UserId = student.UserId,
                                        RoleSpecificId = student.StudentId,
                                        RegistrationNumber = student.RegistrationNumber,
                                        Name = $"{student.FirstName} {student.LastName}",
                                        PhoneNumber = phone,
                                        Email = email,
                                        ProfileImageUrlSmall = student.ProfileImageUrlSmall,
                                        ProfileImageUrlLarge = student.ProfileImageUrlLarge,
                                        IsAlreadyAssigned = true
                                    });
                                    seenStudentIds.Add(student.StudentId);
                                }
                            }
                        }
                    }
                }

                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = false, Message = "Error searching students: " + ex.Message };
            }
        }

        public async Task<ServiceResponse<SearchUserResultDto>> SearchInstituteExactAsync(Guid userId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "Search query is empty." };

            query = query.ToLower().Trim();

            try
            {
                var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
                if (tutor == null) return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "Tutor not found." };

                // Normalize phone for exact matching
                string cleanPhone = query.Replace(" ", "").Replace("-", "");
                string exactPhone = cleanPhone;
                if (cleanPhone.StartsWith("0")) exactPhone = "+94" + cleanPhone.Substring(1);
                else if (!cleanPhone.StartsWith("+") && cleanPhone.Length == 9) exactPhone = "+94" + cleanPhone;

                // Search exact match by Registration Number or Phone Number
                var institutes = await _instituteRepo.GetAllAsync(
                    i => i.IsActive && (
                        i.RegistrationNumber.ToLower() == query || 
                        i.ContactNumber == query || 
                        i.ContactNumber == exactPhone ||
                        (i.User != null && (i.User.PhoneNumber == query || i.User.PhoneNumber == exactPhone))
                    ),
                    includeProperties: "User,User.City"
                );

                var institute = institutes.FirstOrDefault();

                if (institute == null)
                    return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "No active institute found with the exact Reg No or Mobile Number." };

                // Check if already assigned
                var existingAssignment = await _instituteTutorRepo.GetAsync(
                    it => it.InstituteId == institute.InstituteId && it.TutorId == tutor.TutorId);

                // Check if pending request exists
                var pendingRequest = await _joinRequestRepo.GetAsync(
                    r => r.InstituteId == institute.InstituteId && r.TutorId == tutor.TutorId && r.Status == AssignmentStatus.Pending);

                var result = new SearchUserResultDto
                {
                    UserId = institute.UserId,
                    RoleSpecificId = institute.InstituteId,
                    RegistrationNumber = institute.RegistrationNumber,
                    Name = institute.InstituteName,
                    PhoneNumber = institute.User?.PhoneNumber,
                    Email = institute.User?.Email,
                    ProfileImageUrlSmall = institute.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = institute.ProfileImageUrlLarge,
                    IsAlreadyAssigned = existingAssignment != null || pendingRequest != null
                };

                return new ServiceResponse<SearchUserResultDto> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<SearchUserResultDto> { Success = false, Message = "Error searching institute: " + ex.Message };
            }
        }

        public async Task<ServiceResponse<PaginatedResultDto<TutorProfileDto>>> GetAllTutorsAsync(string? searchQuery, int page, int pageSize)
        {
            var response = new ServiceResponse<PaginatedResultDto<TutorProfileDto>>();
            try
            {
                var data = await _tutorRepo.GetAllTutorsAsync(searchQuery, page, pageSize);
                response.Data = data;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching all tutors: " + ex.Message;
            }
            return response;
        }
        public async Task<ServiceResponse<AttendanceHistoryResponseDto>> GetAttendanceHistoryAsync(
            Guid userId, Guid? classId, Guid? instituteId, bool noInstitute, string? searchQuery, int page = 1, int pageSize = 10)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null)
                return new ServiceResponse<AttendanceHistoryResponseDto> { Success = false, Message = "Tutor not found." };

            List<Class> targetClasses;

            if (classId.HasValue && classId.Value != Guid.Empty)
            {
                // A specific class was selected — ignore institute filter
                var cls = await _classRepo.GetAsync(c => c.ClassId == classId.Value && c.TutorId == tutor.TutorId);
                if (cls == null)
                    return new ServiceResponse<AttendanceHistoryResponseDto> { Success = false, Message = "Class not found or access denied." };
                targetClasses = new List<Class> { cls };
            }
            else if (noInstitute)
            {
                // "My Own Place" — classes with no institute
                var all = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId && !c.IsDeleted && c.InstituteId == null);
                targetClasses = all.ToList();
            }
            else if (instituteId.HasValue && instituteId.Value != Guid.Empty)
            {
                // A specific institute was selected
                var all = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId && !c.IsDeleted && c.InstituteId == instituteId.Value);
                targetClasses = all.ToList();
            }
            else
            {
                // All institutes — return all classes for this tutor
                var all = await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId && !c.IsDeleted);
                targetClasses = all.ToList();
            }

            if (!targetClasses.Any())
                return new ServiceResponse<AttendanceHistoryResponseDto>
                {
                    Success = true,
                    Data = new AttendanceHistoryResponseDto()
                };

            var classIds = targetClasses.Select(c => c.ClassId).ToList();

            // Fetch all attendances for these classes (tutor-owned, no institute filter)
            var allAttendances = await _attendanceRepo.GetAllAsync(a => classIds.Contains(a.ClassId));
            var attendancesList = allAttendances.ToList();
            var distinctDates = attendancesList.Select(a => a.Date.Date).Distinct().OrderBy(d => d).ToList();

            // Fetch enrolled students
            var enrollments = await _enrollmentRepo.GetAllAsync(
                e => classIds.Contains(e.ClassId) && e.Status == EnrollmentStatus.Approved,
                includeProperties: "Student");

            var distinctStudents = enrollments
                .Where(e => e.Student != null)
                .Select(e => e.Student)
                .GroupBy(s => s.StudentId)
                .Select(g => g.First())
                .ToList();

            // Build a lookup: studentId -> set of classIds they are enrolled in
            var studentToClassIds = enrollments
                .GroupBy(e => e.StudentId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ClassId).ToHashSet());

            // Fetch user details for phone/email search
            var userIds = distinctStudents.Select(s => s.UserId).Distinct().ToList();
            var allUsers = await _userRepo.GetAllAsync();
            var userDict = allUsers.Where(u => userIds.Contains(u.UserId)).ToDictionary(u => u.UserId);

            // Filter by search query
            var matchedStudents = distinctStudents.Where(student =>
            {
                if (string.IsNullOrWhiteSpace(searchQuery)) return true;
                userDict.TryGetValue(student.UserId, out var u);
                string phone = u?.PhoneNumber ?? "";
                string email = u?.Email ?? "";
                var lq = searchQuery.ToLower().Trim();
                return (student.FirstName != null && student.FirstName.ToLower().Contains(lq))
                    || (student.LastName != null && student.LastName.ToLower().Contains(lq))
                    || (student.RegistrationNumber != null && student.RegistrationNumber.ToLower().Contains(lq))
                    || phone.Contains(lq)
                    || email.ToLower().Contains(lq);
            }).ToList();

            int totalCount = matchedStudents.Count;
            var pagedStudents = matchedStudents
                .OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var rowDtos = pagedStudents.Select(student =>
            {
                userDict.TryGetValue(student.UserId, out var u);
                var studentAttendances = attendancesList.Where(a => a.StudentId == student.StudentId).ToList();
                var record = distinctDates.ToDictionary(date => date, date =>
                    studentAttendances.Any(a => a.Date.Date == date && a.IsPresent));

                // Per-student conducted dates: only dates when THIS student's class(es) ran
                var myClassIds = studentToClassIds.GetValueOrDefault(student.StudentId, new HashSet<Guid>());
                var myClassConductedDates = attendancesList
                    .Where(a => myClassIds.Contains(a.ClassId))
                    .Select(a => a.Date.Date)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                return new StudentAttendanceRowDto
                {
                    StudentId = student.StudentId,
                    Name = $"{student.FirstName} {student.LastName}".Trim(),
                    RegistrationNumber = student.RegistrationNumber ?? "",
                    MobileNumber = u?.PhoneNumber ?? "",
                    AttendanceRecord = record,
                    ClassConductedDates = myClassConductedDates
                };
            }).ToList();

            var responseDto = new AttendanceHistoryResponseDto
            {
                ConductedDates = distinctDates,
                Students = rowDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalStudentCount = totalCount,
                TotalReceived = 0,
                TotalDue = 0
            };

            return new ServiceResponse<AttendanceHistoryResponseDto> { Success = true, Data = responseDto };
        }

        public async Task<ServiceResponse<IEnumerable<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>>> GetMarkSheetsAsync(Guid userId, Guid? classId, Guid? instituteId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<IEnumerable<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> { Success = false, Message = "Tutor not found" };

            var query = await _markSheetRepo.GetAllAsync(m => m.TutorId == tutor.TutorId && !m.IsDeleted, includeProperties: "Institute,Class");
            
            if (classId.HasValue) query = query.Where(m => m.ClassId == classId.Value);
            if (instituteId.HasValue) query = query.Where(m => m.InstituteId == instituteId.Value);

            var dtos = query.Select(m => new Tutorz.Application.DTOs.MarkSheet.MarkSheetDto
            {
                MarkSheetId = m.MarkSheetId,
                ReferenceNumber = m.ReferenceNumber,
                InstituteId = m.InstituteId,
                InstituteName = m.Institute?.InstituteName,
                ClassId = m.ClassId,
                ClassName = m.Class?.ClassName,
                Grade = m.Class?.Grade,
                Subject = m.Class?.Subject,
                Title = m.Title,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).OrderByDescending(m => m.CreatedAt).ToList();

            return new ServiceResponse<IEnumerable<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> { Success = true, Data = dtos };
        }

        public async Task<ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> GetMarkSheetByIdAsync(Guid userId, Guid markSheetId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = false, Message = "Tutor not found" };

            var m = await _markSheetRepo.GetAsync(ms => ms.MarkSheetId == markSheetId && ms.TutorId == tutor.TutorId && !ms.IsDeleted, includeProperties: "Institute,Class,MarkRecords,MarkRecords.Student");
            if (m == null) return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = false, Message = "Mark sheet not found" };

            var dto = new Tutorz.Application.DTOs.MarkSheet.MarkSheetDto
            {
                MarkSheetId = m.MarkSheetId,
                ReferenceNumber = m.ReferenceNumber,
                InstituteId = m.InstituteId,
                InstituteName = m.Institute?.InstituteName,
                ClassId = m.ClassId,
                ClassName = m.Class?.ClassName,
                Grade = m.Class?.Grade,
                Subject = m.Class?.Subject,
                Title = m.Title,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                MarkRecords = m.MarkRecords.Select(r => new Tutorz.Application.DTOs.MarkSheet.MarkRecordDto
                {
                    MarkRecordId = r.MarkRecordId,
                    StudentId = r.StudentId,
                    StudentName = r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : "",
                    RegistrationNumber = r.Student?.RegistrationNumber,
                    Marks = r.Marks,
                    Medal = r.Medal.ToString()
                }).ToList()
            };

            return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = true, Data = dto };
        }

        private void CalculateMedals(List<MarkRecord> records)
        {
            foreach(var r in records) r.Medal = MedalType.None;
            
            if (records.Count <= 3) return; // No medals if <= 3 students

            var distinctMarks = records.Select(r => r.Marks).Distinct().OrderByDescending(m => m).ToList();
            
            if (distinctMarks.Count > 0)
            {
                var goldMark = distinctMarks[0];
                foreach (var r in records.Where(x => x.Marks == goldMark)) r.Medal = MedalType.Gold;
            }
            if (distinctMarks.Count > 1)
            {
                var silverMark = distinctMarks[1];
                foreach (var r in records.Where(x => x.Marks == silverMark)) r.Medal = MedalType.Silver;
            }
            if (distinctMarks.Count > 2)
            {
                var bronzeMark = distinctMarks[2];
                foreach (var r in records.Where(x => x.Marks == bronzeMark)) r.Medal = MedalType.Bronze;
            }
        }

        public async Task<ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> CreateMarkSheetAsync(Guid userId, Tutorz.Application.DTOs.MarkSheet.CreateMarkSheetDto dto)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = false, Message = "Tutor not found" };

            var cls = await _classRepo.GetAsync(c => c.ClassId == dto.ClassId && c.TutorId == tutor.TutorId);
            if (cls == null) return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = false, Message = "Class not found or access denied" };

            string refNo = "MS" + DateTime.UtcNow.ToString("yyMMddHHmmss");

            var markSheet = new MarkSheet
            {
                MarkSheetId = Guid.NewGuid(),
                ReferenceNumber = refNo,
                TutorId = tutor.TutorId,
                ClassId = dto.ClassId,
                InstituteId = dto.InstituteId,
                Title = dto.Title,
                CreatedAt = DateTime.UtcNow,
                MarkRecords = new List<MarkRecord>()
            };

            foreach (var m in dto.Marks)
            {
                markSheet.MarkRecords.Add(new MarkRecord
                {
                    MarkRecordId = Guid.NewGuid(),
                    MarkSheetId = markSheet.MarkSheetId,
                    StudentId = m.StudentId,
                    Marks = m.Marks,
                    Medal = MedalType.None
                });
            }

            CalculateMedals(markSheet.MarkRecords.ToList());

            await _markSheetRepo.AddAsync(markSheet);
            await _markSheetRepo.SaveChangesAsync();

            return await GetMarkSheetByIdAsync(userId, markSheet.MarkSheetId);
        }

        public async Task<ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> UpdateMarkSheetAsync(Guid userId, Guid markSheetId, Tutorz.Application.DTOs.MarkSheet.UpdateMarkSheetDto dto)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = false, Message = "Tutor not found" };

            var m = await _markSheetRepo.GetAsync(ms => ms.MarkSheetId == markSheetId && ms.TutorId == tutor.TutorId, includeProperties: "MarkRecords");
            if (m == null) return new ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto> { Success = false, Message = "Mark sheet not found" };

            m.Title = dto.Title;
            m.UpdatedAt = DateTime.UtcNow;

            var existingRecords = m.MarkRecords.ToList();
            
            foreach(var er in existingRecords)
            {
                var input = dto.Marks.FirstOrDefault(x => x.StudentId == er.StudentId);
                if (input != null)
                {
                    er.Marks = input.Marks;
                }
            }

            foreach(var input in dto.Marks)
            {
                if (!existingRecords.Any(x => x.StudentId == input.StudentId))
                {
                    m.MarkRecords.Add(new MarkRecord
                    {
                        MarkRecordId = Guid.NewGuid(),
                        MarkSheetId = m.MarkSheetId,
                        StudentId = input.StudentId,
                        Marks = input.Marks,
                        Medal = MedalType.None
                    });
                }
            }

            CalculateMedals(m.MarkRecords.ToList());

            await _markSheetRepo.SaveChangesAsync();

            return await GetMarkSheetByIdAsync(userId, m.MarkSheetId);
        }

        public async Task<ServiceResponse<bool>> DeleteMarkSheetAsync(Guid userId, Guid markSheetId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return new ServiceResponse<bool> { Success = false, Message = "Tutor not found" };

            var m = await _markSheetRepo.GetAsync(ms => ms.MarkSheetId == markSheetId && ms.TutorId == tutor.TutorId && !ms.IsDeleted);
            if (m == null) return new ServiceResponse<bool> { Success = false, Message = "Mark sheet not found" };

            m.IsDeleted = true;
            m.UpdatedAt = DateTime.UtcNow;
            await _markSheetRepo.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = true };
        }

        public async Task<ServiceResponse<TutorDashboardStatsDto>> GetDashboardStatsAsync(Guid userId)
        {
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null)
                return ServiceResponse<TutorDashboardStatsDto>.ErrorResponse("Tutor not found.");

            // 1. Total Students
            var enrollments = await _enrollmentRepo.GetAllAsync(e => e.Class.TutorId == tutor.TutorId && e.Status == Tutorz.Domain.Entities.EnrollmentStatus.Approved, includeProperties: "Class");
            var totalStudents = enrollments.Select(e => e.StudentId).Distinct().Count();

            // 2. Active Classes
            var activeClasses = (await _classRepo.GetAllAsync(c => c.TutorId == tutor.TutorId && c.IsActive)).Count();

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            // 3. Monthly Income
            // Using BaseFee or AmountPaid as fallback, though TuitionAmount is preferred. Wait, TuitionAmount is calculated.
            var monthlyPayments = await _paymentRepo.GetAllAsync(p => p.Class.TutorId == tutor.TutorId 
                && p.Month == currentMonth 
                && p.Year == currentYear 
                && (p.Status == "Paid" || p.Status == "PAID"), includeProperties: "Class");
                
            var monthlyIncome = monthlyPayments.Sum(p => p.TuitionAmount ?? p.AmountPaid);

            // 4. Pending Withdrawals (Only for classes tied to an institute)
            var institutePayments = await _paymentRepo.GetAllAsync(p => p.Class.TutorId == tutor.TutorId 
                && p.InstituteId != null 
                && (p.Status == "Paid" || p.Status == "PAID"), includeProperties: "Class");
            var totalInstituteEarnings = institutePayments.Sum(p => p.TuitionAmount ?? p.AmountPaid);

            var withdrawals = await _withdrawalRepo.GetAllAsync(w => w.TutorId == tutor.TutorId && w.InstituteId != null);
            var totalWithdrawn = withdrawals.Sum(w => w.WithdrawalAmount);

            var pendingWithdrawals = totalInstituteEarnings - totalWithdrawn;

            return new ServiceResponse<TutorDashboardStatsDto>
            {
                Success = true,
                Data = new TutorDashboardStatsDto
                {
                    TotalStudents = totalStudents,
                    ActiveClasses = activeClasses,
                    MonthlyIncome = monthlyIncome,
                    PendingWithdrawals = pendingWithdrawals > 0 ? pendingWithdrawals : 0
                }
            };
        }
    }
}