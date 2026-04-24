using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Student;
using Tutorz.Domain.Entities;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Application.Interfaces
{
    public interface IStudentRepository : IGenericRepository<Student>
    {
        Task<PaginatedResultDto<ClassSearchDto>> SearchClassesAsync(string? grade, string? searchTerm, Guid? studentId = null, int? districtId = null, int? cityId = null, int page = 1, int pageSize = 10);
        Task<string> RequestJoinClassAsync(Guid studentId, Guid classId);
        Task<string> LeaveClassAsync(Guid studentId, Guid classId);
        Task<List<StudentClassDto>> GetJoinedClassesAsync(Guid studentId);
        Task<IEnumerable<Attendance>> GetAttendancesAsync(Guid studentId);
        Task<StudentAttendanceHistoryResponseDto> GetStudentAttendanceHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, DateTime? date);
        Task<StudentPaymentHistoryResponseDto> GetStudentPaymentHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, string? monthYear, int page, int pageSize);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByClassAsync(Guid classId);
        Task<PaginatedResultDto<StudentProfileDto>> GetAllStudentsAsync(string? searchQuery, int page, int pageSize);
    }
}
