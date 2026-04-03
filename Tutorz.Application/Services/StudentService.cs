using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Student;
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

        public StudentService(
            IStudentRepository studentRepo,
            IUserRepository userRepo,
            IProfilePictureService profilePictureService,
            IGenericRepository<City> cityRepo,
            IGenericRepository<District> districtRepo)
        {
            _studentRepo = studentRepo;
            _userRepo = userRepo;
            _profilePictureService = profilePictureService;
            _cityRepo = cityRepo;
            _districtRepo = districtRepo;
        }

        public async Task<ServiceResponse<List<ClassSearchDto>>> SearchClassesAsync(string grade, string searchTerm)
        {
            var response = new ServiceResponse<List<ClassSearchDto>>();
            try
            {
                var classes = await _studentRepo.SearchClassesAsync(grade, searchTerm);
                response.Data = classes;
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

            return new ServiceResponse<StudentProfileDto> { Success = true, Data = dto };
        }
        public async Task<ServiceResponse<StudentProfileDto>> UpdateProfileAsync(Guid studentId, UpdateStudentProfileDto dto)
        {
            // Get the student using the Generic Repository
            var student = await _studentRepo.GetAsync(s => s.StudentId == studentId);

            if (student == null)
                return new ServiceResponse<StudentProfileDto> { Success = false, Message = "Student not found." };

            // Entity Framework tracks these changes automatically
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.SchoolName = dto.SchoolName;
            student.Grade = dto.Grade;
            student.ParentName = dto.ParentName;
            student.DateOfBirth = dto.DateOfBirth;
            student.Address = dto.Address;

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
    }
}
