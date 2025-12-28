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


        public StudentService(IStudentRepository studentRepo)
        {
            _studentRepo = studentRepo;
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
                response.Message = "Error joining class: " + ex.Message;
            }
            return response;
        }

        public async Task<ServiceResponse<StudentProfileDto>> GetProfileAsync(Guid studentId)
        {
            // USE YOUR EXISTING REPOSITORY METHOD
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
                Email = student.User?.Email ?? "" // Safe access since we included "User"
            };

            return new ServiceResponse<StudentProfileDto> { Success = true, Data = dto };
        }
        public async Task<ServiceResponse<StudentProfileDto>> UpdateProfileAsync(Guid studentId, UpdateStudentProfileDto dto)
        {
            // 1. Get the student using the Generic Repository
            var student = await _studentRepo.GetAsync(s => s.StudentId == studentId);

            if (student == null)
                return new ServiceResponse<StudentProfileDto> { Success = false, Message = "Student not found." };

            // 2. Update the fields in memory
            // Entity Framework tracks these changes automatically
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.SchoolName = dto.SchoolName;
            student.Grade = dto.Grade;
            student.ParentName = dto.ParentName;
            student.DateOfBirth = dto.DateOfBirth;

            // 3. Save changes using the Generic Repository method
            await _studentRepo.SaveChangesAsync();

            // 4. Return the fresh data
            return await GetProfileAsync(studentId);
        }
    }
}
