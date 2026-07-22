using Tutorz.Application.DTOs.Student;
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
        Task<ServiceResponse<TutorDashboardStatsDto>> GetDashboardStatsAsync(Guid userId);
        Task<bool> AddStudentToClassAsync(Guid userId, AddStudentRequest request);
        Task DeleteClassAsync(Guid classId, Guid userId);
        Task<ServiceResponse<BatchOperationResponse>> RemoveAllStudentsFromClassAsync(Guid classId, Guid userId, int batchSize = 10);
        Task<ServiceResponse<BatchOperationResponse>> ReassignAllStudentsAsync(Guid oldClassId, Guid newClassId, Guid userId, int batchSize = 10);
        Task<ServiceResponse<bool>> DropStudentFromClassAsync(Guid classId, Guid studentId, Guid userId);
        Task<ServiceResponse<bool>> ReassignStudentToClassAsync(Guid studentId, Guid oldClassId, Guid newClassId, Guid userId);
        Task<ServiceResponse<TutorProfileDto>> GetTutorProfileAsync(Guid userId);
        Task<ServiceResponse<TutorProfileDto>> UpdateTutorProfileAsync(Guid userId, UpdateTutorProfileDto request);
        Task<List<StudentRequestDto>> GetStudentRequestsAsync(Guid userId);
        Task<bool> ProcessStudentRequestsAsync(ProcessRequestDto request);
        Task<StudentFullProfileDto> GetStudentProfileAsync(Guid studentId);
        Task<ServiceResponse<IEnumerable<ClassDto>>> GetStudentClassesForTutorAsync(Guid userId, Guid studentId);
        Task<ServiceResponse<bool>> MarkAttendanceAsync(Guid userId, Tutorz.Application.DTOs.Institute.MarkAttendanceDto dto);
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsGlobalAsync(string query);

        // Institute Join Requests
        Task<ServiceResponse<bool>> SendInstituteRequestAsync(Guid tutorId, Guid instituteId);
        Task<ServiceResponse<IEnumerable<JoinRequestDto>>> GetInstituteRequestsAsync(Guid tutorId);
        Task<ServiceResponse<bool>> ProcessInstituteRequestAsync(Guid tutorId, Guid requestId, string action);

        // Joined Institutes
        Task<ServiceResponse<IEnumerable<InstituteDto>>> GetJoinedInstitutesAsync(Guid userId);
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid tutorId, string query);
        Task<ServiceResponse<PaginatedResultDto<StudentProfileDto>>> GetTutorStudentsAsync(Guid userId, Guid? instituteId, Guid? classId, string searchQuery = "", int page = 1, int pageSize = 10);
        Task<ServiceResponse<SearchUserResultDto>> SearchInstituteExactAsync(Guid tutorId, string query);
        Task<ServiceResponse<PaginatedResultDto<TutorProfileDto>>> GetAllTutorsAsync(string? searchQuery, int page, int pageSize);

        // Attendance History (for Tutor's own classes)
        Task<ServiceResponse<AttendanceHistoryResponseDto>> GetAttendanceHistoryAsync(Guid userId, Guid? classId, Guid? instituteId, bool noInstitute, string? searchQuery, int page, int pageSize);
        
        // MarkSheets
        Task<ServiceResponse<IEnumerable<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>>> GetMarkSheetsAsync(Guid userId, Guid? classId, Guid? instituteId);
        Task<ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> GetMarkSheetByIdAsync(Guid userId, Guid markSheetId);
        Task<ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> CreateMarkSheetAsync(Guid userId, Tutorz.Application.DTOs.MarkSheet.CreateMarkSheetDto dto);
        Task<ServiceResponse<Tutorz.Application.DTOs.MarkSheet.MarkSheetDto>> UpdateMarkSheetAsync(Guid userId, Guid markSheetId, Tutorz.Application.DTOs.MarkSheet.UpdateMarkSheetDto dto);
        Task<ServiceResponse<bool>> DeleteMarkSheetAsync(Guid userId, Guid markSheetId);
    }
}
