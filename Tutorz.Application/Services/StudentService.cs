using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Domain.Entities;
using Tutorz.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tutorz.Application.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IUserRepository _userRepo;
        private readonly IProfilePictureService _profilePictureService;
        private readonly IGenericRepository<City> _cityRepo;
        private readonly IGenericRepository<District> _districtRepo;
        private readonly ITutorRepository _tutorRepo;

        public StudentService(
            IStudentRepository studentRepo,
            IUserRepository userRepo,
            IProfilePictureService profilePictureService,
            IGenericRepository<City> cityRepo,
            IGenericRepository<District> districtRepo,
            ITutorRepository tutorRepo)
        {
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _profilePictureService = profilePictureService;
            _cityRepo = cityRepo;
            _districtRepo = districtRepo;
            _tutorRepo = tutorRepo;
        }

        public async Task<ServiceResponse<PaginatedResultDto<ClassSearchDto>>> SearchClassesAsync(string? grade, string? searchTerm, Guid? studentId = null, int? districtId = null, int? cityId = null, int page = 1, int pageSize = 10)
        {
            var response = new ServiceResponse<PaginatedResultDto<ClassSearchDto>>();
            try
            {
                var paginatedData = await _studentRepo.SearchClassesAsync(grade, searchTerm, studentId, districtId, cityId, page, pageSize);
                response.Data = paginatedData;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error searching classes: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<string>> RequestJoinClassAsync(Guid studentId, Guid classId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var result = await _studentRepo.RequestJoinClassAsync(studentId, classId);

                if (result == "Success")
                {
                    response.Success = true;
                    response.Data = "Request Sent";
                    response.Message = "Request to join class sent successfully.";
                }
                else
                {
                    response.Success = false;
                    response.Message = result;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;

                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                response.Message = "Error joining class: " + innerMessage;
            }
            return response;
        }

        public async Task<ServiceResponse<string>> LeaveClassAsync(Guid studentId, Guid classId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var result = await _studentRepo.LeaveClassAsync(studentId, classId);
                if (result == "Success")
                {
                    response.Success = true;
                    response.Data = "Left Class";
                    response.Message = "You have successfully left the class.";
                }
                else
                {
                    response.Success = false;
                    response.Message = result;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error leaving class: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<StudentProfileDto>> GetProfileAsync(Guid studentId)
        {
            // This finds the student AND joins the User table to get the Email
            var student = await _studentRepo.GetAsync(
                expression: s => s.StudentId == studentId,
                includeProperties: "User"
            );

            if (student == null)
                return new ServiceResponse<StudentProfileDto> { Success = false, Message = "Student not found." };

            var dto = new StudentProfileDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                SchoolName = student.SchoolName,
                Grade = student.Grade,
                ParentName = student.ParentName,
                DateOfBirth = student.DateOfBirth,
                RegistrationNumber = student.RegistrationNumber,
                Email = student.User?.Email ?? "",
                PhoneNumber = student.User?.PhoneNumber ?? "",
                ProfileImageUrlSmall = student.ProfileImageUrlSmall,
                ProfileImageUrlLarge = student.ProfileImageUrlLarge,
                Address = student.Address,
                CityId = student.User?.CityId
            };

            if (dto.CityId.HasValue)
            {
                var city = await _cityRepo.GetAsync(c => c.Id == dto.CityId.Value);
                if (city != null)
                {
                    dto.DistrictId = city.DistrictId;
                    var district = await _districtRepo.GetAsync(d => d.Id == city.DistrictId);
                    if (district != null)
                    {
                        dto.ProvinceId = district.ProvinceId;
                    }
                }
            }

            // Fetch siblings for account switching
            if (student.UserId != Guid.Empty)
            {
                var allStudents = await _studentRepo.GetAllAsync(s => s.UserId == student.UserId);
                dto.Profiles = allStudents.Select(s => new Tutorz.Application.DTOs.Auth.StudentProfileDto
                {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    Grade = s.Grade,
                    IsPrimary = s.IsPrimary,
                    ProfileImageUrlSmall = s.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = s.ProfileImageUrlLarge
                }).ToList();
            }

            return new ServiceResponse<StudentProfileDto> { Success = true, Data = dto };
        }
        public async Task<ServiceResponse<StudentProfileDto>> UpdateProfileAsync(Guid studentId, UpdateStudentProfileDto dto)
        {
            // Get the student using the Generic Repository
            var student = await _studentRepo.GetAsync(s => s.StudentId == studentId);

            if (student == null)
                return new ServiceResponse<StudentProfileDto> { Success = false, Message = "Student not found." };

            // Entity Framework tracks these changes automatically.
            // Only update a field if a new value was actually provided.
            // This preserves existing data when (e.g.) only a photo is uploaded.
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            if (dto.SchoolName != null) student.SchoolName = dto.SchoolName;
            if (dto.Grade != null) student.Grade = dto.Grade;
            if (dto.ParentName != null) student.ParentName = dto.ParentName;
            student.DateOfBirth = dto.DateOfBirth;
            if (dto.Address != null) student.Address = dto.Address;

            if (dto.ProfilePicture != null)
            {
                try
                {
                    var (smallUrl, largeUrl) = await _profilePictureService.UploadProfilePictureAsync(
                        student.StudentId,
                        student.RegistrationNumber,
                        "Student",
                        dto.ProfilePicture
                    );

                    student.ProfileImageUrlSmall = smallUrl;
                    student.ProfileImageUrlLarge = largeUrl;
                }
                catch (Exception ex)
                {
                    return new ServiceResponse<StudentProfileDto> 
                    { 
                        Success = false, 
                        Message = $"Failed to upload profile picture: {ex.Message}" 
                    };
                }
            }

            // Save changes using the Generic Repository method
            await _studentRepo.SaveChangesAsync();

            // Update user City location if provided
            if (dto.CityId.HasValue && student.UserId != Guid.Empty)
            {
                var user = await _userRepo.GetAsync(u => u.UserId == student.UserId);
                if (user != null)
                {
                    user.CityId = dto.CityId;
                    await _userRepo.SaveChangesAsync();
                }
            }

            // Return the fresh data
            return await GetProfileAsync(studentId);
        }

        public async Task<ServiceResponse<List<StudentClassDto>>> GetJoinedClassesAsync(Guid studentId)
        {
            var response = new ServiceResponse<List<StudentClassDto>>();
            try
            {
                var classes = await _studentRepo.GetJoinedClassesAsync(studentId);
                response.Data = classes;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching joined classes: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<IEnumerable<StudentClassDto>>> GetClassesByDateAsync(Guid studentId, DateTime date)
        {
            var response = new ServiceResponse<IEnumerable<StudentClassDto>>();
            try
            {
                var allClasses = await _studentRepo.GetJoinedClassesAsync(studentId);
                
                string dayOfWeek = date.DayOfWeek.ToString();
                var dateOnly = date.Date;

                var filtered = allClasses.Where(c =>
                    (c.ClassType == "Class" && c.DayOfWeek != null &&
                     c.DayOfWeek.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase)) ||
                    (c.ClassType != "Class" && c.Date.HasValue && c.Date.Value.Date == dateOnly)
                ).ToList();

                response.Data = filtered;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching classes by date: " + ex.Message;
            }
            return response;
        }
        public async Task<ServiceResponse<StudentAttendanceHistoryResponseDto>> GetAttendanceHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, DateTime? date)
        {
            var response = new ServiceResponse<StudentAttendanceHistoryResponseDto>();
            try
            {
                var data = await _studentRepo.GetStudentAttendanceHistoryAsync(studentId, tutorId, classId, date);
                response.Data = data;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching attendance history: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<StudentPaymentHistoryResponseDto>> GetStudentPaymentHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, string? monthYear, int page, int pageSize)
        {
            var response = new ServiceResponse<StudentPaymentHistoryResponseDto>();
            try
            {
                var data = await _studentRepo.GetStudentPaymentHistoryAsync(studentId, tutorId, classId, monthYear, page, pageSize);
                response.Data = data;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error fetching student payment history: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid studentId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = new List<SearchUserResultDto>() };

            query = query.ToLower().Trim();

            try
            {
                // Get all approved classes the student is in
                var joinedClasses = await _studentRepo.GetJoinedClassesAsync(studentId);
                
                // Extract unique tutors and their user details
                var results = new List<SearchUserResultDto>();
                var seenTutorIds = new HashSet<Guid>();

                foreach (var cls in joinedClasses)
                {
                    if (cls.TutorId != Guid.Empty && !seenTutorIds.Contains(cls.TutorId))
                    {
                        var tutor = await _tutorRepo.GetAsync(
                            t => t.TutorId == cls.TutorId,
                            includeProperties: "User"
                        );

                        if (tutor != null)
                        {
                            string fullName = $"{tutor.FirstName} {tutor.LastName}".ToLower();
                            string regNo = tutor.RegistrationNumber?.ToLower() ?? "";
                            string phone = tutor.User?.PhoneNumber ?? "";
                            string email = tutor.User?.Email?.ToLower() ?? "";

                            if (fullName.Contains(query) || regNo.Contains(query) || phone.Contains(query) || email.Contains(query))
                            {
                                results.Add(new SearchUserResultDto
                                {
                                    UserId = tutor.UserId,
                                    RoleSpecificId = tutor.TutorId,
                                    RegistrationNumber = tutor.RegistrationNumber,
                                    Name = $"{tutor.FirstName} {tutor.LastName}",
                                    PhoneNumber = phone,
                                    Email = email,
                                    ProfileImageUrlSmall = tutor.ProfileImageUrlSmall,
                                    ProfileImageUrlLarge = tutor.ProfileImageUrlLarge,
                                    IsAlreadyAssigned = true
                                });
                                seenTutorIds.Add(tutor.TutorId);
                            }
                        }
                    }
                }

                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = true, Data = results };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<IEnumerable<SearchUserResultDto>> { Success = false, Message = "Error searching tutors: " + ex.Message };
            }
        }
    }
}
