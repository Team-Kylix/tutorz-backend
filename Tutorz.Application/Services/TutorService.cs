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
        private readonly IUserRepository _userRepo;
        private readonly IInstituteJoinRequestRepository _joinRequestRepo;
        private readonly IInstituteTutorRepository _instituteTutorRepo;
        private readonly IInstituteRepository _instituteRepo;
        private readonly IProfilePictureService _profilePictureService;
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<District> _districtRepo;

        public TutorService(
            ITutorRepository tutorRepo,
            IGenericRepository<Class> classRepo,
            IStudentRepository studentRepo,
            IUserRepository userRepo,
            IInstituteJoinRequestRepository joinRequestRepo,
            IInstituteTutorRepository instituteTutorRepo,
            IInstituteRepository instituteRepo,
            IProfilePictureService profilePictureService,
            IGenericRepository<City> cityRepo,
            IGenericRepository<District> districtRepo)
        {
            _tutorRepo = tutorRepo;
            _classRepo = classRepo;
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _joinRequestRepo = joinRequestRepo;
            _instituteTutorRepo = instituteTutorRepo;
            _instituteRepo = instituteRepo;
            _profilePictureService = profilePictureService;
            _cityRepo = cityRepo;
            _districtRepo = districtRepo;
        }

        public async Task<ClassDto> CreateClassAsync(Guid userId, CreateClassRequest request)
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
                        throw new Exception($"Time Crash! This time overlaps with your '{existing.ClassType}': {existing.Subject} ({existing.StartTime} - {existing.EndTime}).");
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
                            throw new Exception($"Cannot create class — {occupyingTutor}'s class already occupies {request.HallName} from {hc.StartTime} to {hc.EndTime} on this day.");
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

            return MapToDto(newClass);
        }

        public async Task<ClassDto> UpdateClassAsync(Guid classId, Guid userId, CreateClassRequest request)
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
                        throw new Exception($"Cannot update class — {occupyingTutor}'s class already occupies {request.HallName} from {hc.StartTime} to {hc.EndTime} on this day.");
                    }
                }
            }

            await _classRepo.SaveChangesAsync();
            return MapToDto(existingClass);
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
            var student = await _studentRepo.GetAsync(s => s.UserId.ToString() == request.StudentRegistrationNumber);
            if (student == null) throw new Exception("Student not found.");

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
    }
}