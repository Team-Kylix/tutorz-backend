using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class AttendanceRepository : GenericRepository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<bool> HasAttendanceAsync(Guid studentId, Guid classId, DateTime date)
        {
            return await _context.Attendances
                .AnyAsync(a => a.StudentId == studentId && 
                               a.ClassId == classId && 
                               a.Date.Date == date.Date);
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesByClassAndDateAsync(Guid classId, DateTime date)
        {
            return await _context.Attendances
                .Include(a => a.Student)
                .ThenInclude(s => s.User)
                .Where(a => a.ClassId == classId && 
                            a.Date.Date == date.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetStudentAttendancesAsync(Guid studentId, Guid? instituteId)
        {
            return await _context.Attendances
                .Include(a => a.Class)
                .Where(a => a.StudentId == studentId && 
                            a.InstituteId == instituteId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }
    }
}
