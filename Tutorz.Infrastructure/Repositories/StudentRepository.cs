using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;


namespace Tutorz.Infrastructure.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<List<ClassSearchDto>> SearchClassesAsync(string grade, string searchTerm)
        {
            var db = _context as TutorzDbContext;

            var query = db.Classes
                .Include(c => c.Tutor)
                .Where(c => c.IsActive);

            // Filter by Grade
            if (!string.IsNullOrEmpty(grade))
            {
                query = query.Where(c => c.Grade == grade);
            }

            // Filter by Search Term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string term = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Subject.ToLower().Contains(term) ||
                    // Access names directly from Tutor
                    (c.Tutor.FirstName + " " + c.Tutor.LastName).ToLower().Contains(term) ||
                    // Search by RegistrationNumber (string)
                    c.Tutor.RegistrationNumber.ToLower().Contains(term)
                );
            }

            // Select into DTO
            return await query.Select(c => new ClassSearchDto
            {
                Id = c.ClassId,
                Subject = c.Subject,
                Grade = c.Grade,
                // Map names from Tutor
                TutorName = c.Tutor.FirstName + " " + c.Tutor.LastName,
                // Map the string ID (RegistrationNumber)
                TutorId = c.Tutor.RegistrationNumber,
                Bio = c.Tutor.Bio,
                Fee = c.Fee,
                DayOfWeek = c.DayOfWeek,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                ClassType = c.ClassType,
                Status = c.IsActive ? "Active" : "Inactive"
            }).ToListAsync();
        }

        public async Task<string> RequestJoinClassAsync(Guid studentId, Guid classId)
        {
            var db = _context as TutorzDbContext;

            // Check existing enrollment
            var existing = await db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.ClassId == classId);

            if (existing != null)
            {
                if (existing.Status == EnrollmentStatus.Pending) return "Request already pending.";
                if (existing.Status == EnrollmentStatus.Approved) return "Already enrolled.";

                return "Request already processed.";
            }

            // Create Enrollment
            var enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                ClassId = classId,
                Status = EnrollmentStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            db.Enrollments.Add(enrollment);
            await db.SaveChangesAsync();

            return "Success";
        }
    }
}
