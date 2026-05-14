using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.DTOs.Institute;

namespace Tutorz.Application.Interfaces
{
    public interface ITutorService
    {
        Task<ServiceResponse<ClassDto>> CreateClassAsync(Guid userId, CreateClassRequest request);
        Task<ServiceResponse<ClassDto>> UpdateClassAsync(Guid classId, Guid userId, CreateClassRequest request);
        Task<List<ClassDto>> GetClassesAsync(Guid userId);
        Task<bool> AddStudentToClassAsync(Guid userId, AddStudentRequest request);
        Task DeleteClassAsync(Guid classId, Guid userId);
        Task<ServiceResponse<TutorProfileDto>> GetTutorProfileAsync(Guid userId);
        Task<ServiceResponse<TutorProfileDto>> UpdateTutorProfileAsync(Guid userId, UpdateTutorProfileDto request);
        Task<List<StudentRequestDto>> GetStudentRequestsAsync(Guid userId);
        Task<bool> ProcessStudentRequestsAsync(ProcessRequestDto request);
        Task<StudentFullProfileDto> GetStudentProfileAsync(Guid studentId);

        // Institute Join Requests
        Task<ServiceResponse<bool>> SendInstituteRequestAsync(Guid tutorId, Guid instituteId);
        Task<ServiceResponse<IEnumerable<JoinRequestDto>>> GetInstituteRequestsAsync(Guid tutorId);
        Task<ServiceResponse<bool>> ProcessInstituteRequestAsync(Guid tutorId, Guid requestId, string action);

        // Joined Institutes
        Task<ServiceResponse<IEnumerable<InstituteDto>>> GetJoinedInstitutesAsync(Guid userId);
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid tutorId, string query);
        Task<ServiceResponse<SearchUserResultDto>> SearchInstituteExactAsync(Guid tutorId, string query);
        Task<ServiceResponse<PaginatedResultDto<TutorProfileDto>>> GetAllTutorsAsync(string? searchQuery, int page, int pageSize);

        // Attendance History (for Tutor's own classes)
        Task<ServiceResponse<AttendanceHistoryResponseDto>> GetAttendanceHistoryAsync(Guid userId, Guid? classId, Guid? instituteId, bool noInstitute, string? searchQuery, int page, int pageSize);
    }
}