using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class TutorRepository : GenericRepository<Tutor>, ITutorRepository
    {
        private readonly TutorzDbContext _context;

        public TutorRepository(TutorzDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<TutorProfileDto> GetTutorProfileAsync(Guid userId)
        {
            // We cast the base Context to your specific TutorzDbContext to access the "Users" table
            // or use _context directly since we saved it
            return await (from t in _context.Tutors
                          join u in _context.Users on t.UserId equals u.UserId
                          where t.UserId == userId
                          select new TutorProfileDto
                          {
                              FirstName = t.FirstName,
                              LastName = t.LastName,
                              Bio = t.Bio,
                              BankAccountNumber = t.BankAccountNumber,
                              BankName = t.BankName,
                              RegistrationNumber = t.RegistrationNumber,
                              Email = u.Email,
                              PhoneNumber = u.PhoneNumber
                          }).FirstOrDefaultAsync();
        }

        public async Task<List<StudentRequestDto>> GetPendingRequestsAsync(Guid tutorUserId)
        {
            // Get the Tutor entity associated with the logged-in UserID
            var tutor = await _context.Tutors
                .FirstOrDefaultAsync(t => t.UserId == tutorUserId);

            if (tutor == null) return new List<StudentRequestDto>();

            // Fetch Enrollments
            return await _context.Enrollments
                .Include(e => e.Student).ThenInclude(s => s.User)
                .Include(e => e.Class)
                .Where(e => e.Class.TutorId == tutor.TutorId && e.Status == EnrollmentStatus.Pending)
                .Select(e => new StudentRequestDto
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    Name = e.Student.FirstName + " " + e.Student.LastName,
                    RegNo = e.Student.RegistrationNumber,
                    Grade = e.Student.Grade,
                    Mobile = e.Student.User.PhoneNumber,
                    Email = e.Student.User.Email,
                    TargetClass = !string.IsNullOrEmpty(e.Class.ClassName) ? e.Class.ClassName : e.Class.Subject,
                    ClassType = e.Class.ClassType,
                    RequestedAt = e.RequestedAt
                })
                .OrderByDescending(e => e.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<Enrollment>> GetEnrollmentsByIdsAsync(List<Guid> enrollmentIds)
        {
            return await _context.Enrollments
                .Where(e => enrollmentIds.Contains(e.Id))
                .ToListAsync();
        }

        public async Task UpdateEnrollmentsAsync(List<Enrollment> enrollments)
        {
            if (enrollments != null && enrollments.Any())
            {
                _context.Enrollments.UpdateRange(enrollments);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<StudentFullProfileDto> GetStudentProfileForTutorAsync(Guid studentId)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null) return null;

            return new StudentFullProfileDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                RegistrationNumber = student.RegistrationNumber,
                Grade = student.Grade,
                SchoolName = student.SchoolName,
                ParentName = student.ParentName,
                Mobile = student.User?.PhoneNumber,
                Email = student.User?.Email,
                DateOfBirth = student.DateOfBirth
            };
        }
    }
}