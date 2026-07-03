using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Institute;

namespace Tutorz.Application.Interfaces
{
    public interface IStudentService
    {
        Task<ServiceResponse<PaginatedResultDto<ClassSearchDto>>> SearchClassesAsync(string? grade, string? searchTerm, Guid? studentId = null, int? provinceId = null, int? districtId = null, int? cityId = null, int page = 1, int pageSize = 10);
        Task<ServiceResponse<string>> RequestJoinClassAsync(Guid studentId, Guid classId);
        Task<ServiceResponse<string>> LeaveClassAsync(Guid studentId, Guid classId);
        Task<ServiceResponse<StudentProfileDto>> GetProfileAsync(Guid studentId);
        Task<ServiceResponse<StudentProfileDto>> UpdateProfileAsync(Guid studentId, UpdateStudentProfileDto dto);
        Task<ServiceResponse<List<StudentClassDto>>> GetJoinedClassesAsync(Guid studentId);
        Task<ServiceResponse<IEnumerable<StudentClassDto>>> GetClassesByDateAsync(Guid studentId, DateTime date);
        Task<ServiceResponse<StudentAttendanceHistoryResponseDto>> GetAttendanceHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, DateTime? date);
        Task<ServiceResponse<StudentPaymentHistoryResponseDto>> GetStudentPaymentHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, string? monthYear, int page, int pageSize);
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid studentId, string query);
        Task<ServiceResponse<PaginatedResultDto<StudentProfileDto>>> GetAllStudentsAsync(string? searchQuery, int page, int pageSize);
        
        Task<ServiceResponse<IEnumerable<Tutorz.Application.DTOs.MarkSheet.StudentMarkRecordResponseDto>>> GetStudentMarksAsync(Guid userId);
        Task<ServiceResponse<int>> GetStudentMedalsCountAsync(Guid userId);
    }
}
