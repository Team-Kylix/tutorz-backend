using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Interfaces
{
    public interface ITutorService
    {
        Task<ClassDto> CreateClassAsync(Guid userId, CreateClassRequest request);
        Task<ClassDto> UpdateClassAsync(Guid classId, Guid userId, CreateClassRequest request);
        Task<List<ClassDto>> GetClassesAsync(Guid userId);
        Task<bool> AddStudentToClassAsync(Guid userId, AddStudentRequest request);
        Task DeleteClassAsync(Guid classId, Guid userId);
        Task<ServiceResponse<TutorProfileDto>> GetTutorProfileAsync(Guid userId);
        Task<ServiceResponse<TutorProfileDto>> UpdateTutorProfileAsync(Guid userId, TutorProfileDto request);
        Task<List<StudentRequestDto>> GetStudentRequestsAsync(Guid userId);
        Task<bool> ProcessStudentRequestsAsync(ProcessRequestDto request);
        Task<StudentFullProfileDto> GetStudentProfileAsync(Guid studentId);
    }
}