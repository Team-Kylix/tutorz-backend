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

        public InstituteService(
            IInstituteRepository instituteRepository,
            IStudentRepository studentRepository,
            ITutorRepository tutorRepository,
            IUserRepository userRepository,
            IInstituteStudentRepository instituteStudentRepository,
            IInstituteTutorRepository instituteTutorRepository,
            IInstituteJoinRequestRepository joinRequestRepository,
            IGenericRepository<Class> classRepository,
            IGenericRepository<Enrollment> enrollmentRepository)
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
                CityId = (institute.User != null) ? institute.User.CityId : null
            };

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
            institute.UpdatedDate = DateTime.UtcNow;

            await _instituteRepository.SaveChangesAsync();

            
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
                includeProperties: "Tutor"
            );

            var dtos = requests.Select(r => new JoinRequestDto
            {
                RequestId = r.Id,
                InstituteId = r.InstituteId,
                TutorId = r.TutorId,
                TutorName = r.Tutor != null ? $"{r.Tutor.FirstName} {r.Tutor.LastName}" : null,
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

            query = query.ToLower();

            // Just a basic search mechanism fetching all and filtering in memory or using where
            // For production, this should preferably use IQueryable or full text search
            var allStudents = await _studentRepository.GetAllAsync();
            
            var matchedStudents = allStudents.Where(s => 
                s.FirstName.ToLower().Contains(query) || 
                s.LastName.ToLower().Contains(query) ||
                s.RegistrationNumber.ToLower().Contains(query)
            ).ToList();

            var assignedStudents = await _instituteStudentRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var assignedStudentIds = assignedStudents.Select(a => a.StudentId).ToHashSet();

            var results = new List<SearchUserResultDto>();
            foreach (var student in matchedStudents)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == student.UserId);
                results.Add(new SearchUserResultDto
                {
                    UserId = student.UserId,
                    RoleSpecificId = student.StudentId,
                    RegistrationNumber = student.RegistrationNumber,
                    Name = $"{student.FirstName} {student.LastName}",
                    PhoneNumber = user?.PhoneNumber,
                    Email = user?.Email,
                    IsAlreadyAssigned = assignedStudentIds.Contains(student.StudentId)
                });
            }

            return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid instituteId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower();

            var allTutors = await _tutorRepository.GetAllAsync();
            var matchedTutors = allTutors.Where(t => 
                t.FirstName.ToLower().Contains(query) || 
                t.LastName.ToLower().Contains(query) ||
                t.RegistrationNumber.ToLower().Contains(query)
            ).ToList();

            var assignedTutors = await _instituteTutorRepository.GetAllAsync(i => i.InstituteId == instituteId);
            var assignedTutorIds = assignedTutors.Select(a => a.TutorId).ToHashSet();

            var results = new List<SearchUserResultDto>();
            foreach (var tutor in matchedTutors)
            {
                var user = await _userRepository.GetAsync(u => u.UserId == tutor.UserId);
                results.Add(new SearchUserResultDto
                {
                    UserId = tutor.UserId,
                    RoleSpecificId = tutor.TutorId,
                    RegistrationNumber = tutor.RegistrationNumber,
                    Name = $"{tutor.FirstName} {tutor.LastName}",
                    PhoneNumber = user?.PhoneNumber,
                    Email = user?.Email,
                    IsAlreadyAssigned = assignedTutorIds.Contains(tutor.TutorId)
                });
            }

            return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
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
                    PhoneNumber = user?.PhoneNumber
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
                    ProfileImageUrl = user?.QrCodeUrl, // Or actual profile image
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
        public async Task<ServiceResponse<PaginatedResultDto<InstituteClassDto>>> GetInstituteClassesAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<PaginatedResultDto<InstituteClassDto>> { Success = false, Message = "Institute not found." };

            var classes = await _classRepository.GetAllAsync(c => c.InstituteId == institute.InstituteId, includeProperties: "Tutor");

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                classes = classes.Where(c => 
                    (c.ClassName != null && c.ClassName.ToLower().Contains(searchQuery)) ||
                    (c.Subject != null && c.Subject.ToLower().Contains(searchQuery)) ||
                    (c.Tutor != null && ((c.Tutor.FirstName != null && c.Tutor.FirstName.ToLower().Contains(searchQuery)) || (c.Tutor.LastName != null && c.Tutor.LastName.ToLower().Contains(searchQuery))))
                ).ToList();
            }

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
                    StudentRegisteredCount = enrollments.Count()
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
                StudentRegisteredCount = 0
            };

            return new ServiceResponse<InstituteClassDto> { Success = true, Data = dto, Message = "Class created successfully." };
        }

        public async Task<ServiceResponse<InstituteClassDto>> UpdateInstituteClassAsync(Guid instituteId, Guid classId, CreateClassRequest request)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<InstituteClassDto> { Success = false, Message = "Institute not found." };

            var existingClass = await _classRepository.GetAsync(c => c.ClassId == classId && c.InstituteId == institute.InstituteId, includeProperties: "Tutor");
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
            existingClass.UpdatedDate = DateTime.UtcNow;

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
                StudentRegisteredCount = enrollments.Count()
            };

            return new ServiceResponse<InstituteClassDto> { Success = true, Data = dto, Message = "Class updated successfully." };
        }

        public async Task<ServiceResponse<bool>> DeleteInstituteClassAsync(Guid instituteId, Guid classId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            var existingClass = await _classRepository.GetAsync(c => c.ClassId == classId && c.InstituteId == institute.InstituteId);
            if (existingClass == null)
                return new ServiceResponse<bool> { Success = false, Message = "Class not found." };

            await _classRepository.DeleteAsync(existingClass);
            return new ServiceResponse<bool> { Success = true, Data = true, Message = "Class deleted successfully." };
        }

        public async Task<ServiceResponse<bool>> ToggleInstituteClassStatusAsync(Guid instituteId, Guid classId)
        {
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == instituteId || i.UserId == instituteId);
            if (institute == null)
                return new ServiceResponse<bool> { Success = false, Message = "Institute not found." };

            var existingClass = await _classRepository.GetAsync(c => c.ClassId == classId && c.InstituteId == institute.InstituteId);
            if (existingClass == null)
                return new ServiceResponse<bool> { Success = false, Message = "Class not found." };

            existingClass.IsActive = !existingClass.IsActive;
            await _classRepository.SaveChangesAsync();

            return new ServiceResponse<bool> { Success = true, Data = existingClass.IsActive, Message = "Class status toggled successfully." };
        }
    }
}