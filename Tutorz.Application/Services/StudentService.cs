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
    }
}
