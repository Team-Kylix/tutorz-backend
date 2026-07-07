using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IAttendanceRepository : IGenericRepository<Attendance>
    {
        Task<bool> HasAttendanceAsync(Guid studentId, Guid classId, DateTime date);
        Task<IEnumerable<Attendance>> GetAttendancesByClassAndDateAsync(Guid classId, DateTime date);
        Task<IEnumerable<Attendance>> GetStudentAttendancesAsync(Guid studentId, Guid? instituteId);
    }
}
