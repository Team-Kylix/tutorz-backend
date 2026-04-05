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
                if (existing.Status == EnrollmentStatus.Pending)  return "Request already pending.";
                if (existing.Status == EnrollmentStatus.Approved)  return "Already enrolled.";

                // If previously Dropped or Rejected, allow the student to re-request
                if (existing.Status == EnrollmentStatus.Dropped || existing.Status == EnrollmentStatus.Rejected)
                {
                    existing.Status = EnrollmentStatus.Pending;
                    existing.RequestedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return "Success";
                }

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

        public async Task<string> LeaveClassAsync(Guid studentId, Guid classId)
        {
            var db = _context as TutorzDbContext;

            var enrollment = await db.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.ClassId == classId);

            if (enrollment == null)
                return "Enrollment not found.";

            if (enrollment.Status == EnrollmentStatus.Dropped)
                return "Already left this class.";

            enrollment.Status = EnrollmentStatus.Dropped;
            await db.SaveChangesAsync();

            return "Success";
        }

        public async Task<Student?> GetStudentWithUserAsync(Guid studentId)
        {
            var db = _context as TutorzDbContext;

            return await db.Students
                .Include(s => s.User) // Load the User to get the Email
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<List<StudentClassDto>> GetJoinedClassesAsync(Guid studentId)
        {
            var db = _context as TutorzDbContext;

            return await db.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Tutor)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Institute)
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Approved)
                .Select(e => new StudentClassDto
                {
                    ClassId = e.ClassId,
                    Subject = e.Class.Subject,
                    Grade = e.Class.Grade,
                    ClassName = e.Class.ClassName,
                    TutorName = e.Class.Tutor.FirstName + " " + e.Class.Tutor.LastName,
                    InstituteName = e.Class.Institute != null ? e.Class.Institute.InstituteName : null,
                    ClassType = e.Class.ClassType,
                    DayOfWeek = e.Class.DayOfWeek,
                    Date = e.Class.Date,
                    StartTime = e.Class.StartTime,
                    EndTime = e.Class.EndTime,
                    HallName = e.Class.HallName,
                    Fee = e.Class.Fee,
                    Status = e.Class.IsActive ? "active" : "inactive",
                    EnrolledAt = e.EnrolledAt
                }).ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendancesAsync(Guid studentId)
        {
            var db = _context as TutorzDbContext;
            return await db.Attendances
                .Include(a => a.Class)
                    .ThenInclude(c => c.Tutor)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        public async Task<StudentAttendanceHistoryResponseDto> GetStudentAttendanceHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, DateTime? date)
        {
            var db = _context as TutorzDbContext;

            // Get valid enrolled classes
            var enrollmentsQuery = db.Enrollments
                .Include(e => e.Class)
                .ThenInclude(c => c.Tutor)
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Approved);

            if (classId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassId == classId.Value);
            }
            if (tutorId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.Class.TutorId == tutorId.Value);
            }

            var enrollments = await enrollmentsQuery.ToListAsync();
            var enrolledClassIds = enrollments.Select(e => e.ClassId).ToList();

            // All attendances taken for these classes (to compute conducted dates)
            var classAttendancesQuery = db.Attendances
                .Where(a => enrolledClassIds.Contains(a.ClassId));

            if (date.HasValue)
            {
                classAttendancesQuery = classAttendancesQuery.Where(a => a.Date.Date == date.Value.Date);
            }

            var classAttendances = await classAttendancesQuery.ToListAsync();

            var distinctConductedDates = classAttendances
                .Select(a => a.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var response = new StudentAttendanceHistoryResponseDto
            {
                ConductedDates = distinctConductedDates,
                Classes = new List<StudentHistoryClassRowDto>()
            };

            int totalDaysHeld = 0;
            int totalDaysAttended = 0;

            foreach (var enrollment in enrollments)
            {
                var cId = enrollment.ClassId;
                
                // For this specific class
                var classSpecificAttendances = classAttendances.Where(a => a.ClassId == cId).ToList();
                
                var classConductedDates = classSpecificAttendances
                    .Select(a => a.Date.Date)
                    .Distinct()
                    .ToList();

                var studentClassAttendances = classSpecificAttendances
                    .Where(a => a.StudentId == studentId && a.IsPresent)
                    .Select(a => a.Date.Date)
                    .ToList();

                totalDaysHeld += classConductedDates.Count;
                totalDaysAttended += studentClassAttendances.Count;

                var row = new StudentHistoryClassRowDto
                {
                    Id = cId,
                    Name = string.IsNullOrEmpty(enrollment.Class.ClassName) ? enrollment.Class.Subject : enrollment.Class.ClassName,
                    RegNo = $"{enrollment.Class.Tutor.FirstName} {enrollment.Class.Tutor.LastName}",
                    Mobile = enrollment.Class.ClassType,
                    AttendanceRecord = new Dictionary<DateTime, bool>()
                };

                // Map the full requested dates
                foreach (var d in distinctConductedDates)
                {
                    // If the class actually occurred on this date, we see if student was present.
                    // If the class didn't occur on this date, we just leave it out of the dictionary or set to false.
                    // The table component expects boolean for dates it displays.
                    // Let's set it if they were present.
                    if (studentClassAttendances.Contains(d))
                    {
                        row.AttendanceRecord[d] = true;
                    }
                }

                response.Classes.Add(row);
            }

            response.DaysHeld = totalDaysHeld;
            response.DaysAttended = totalDaysAttended;
            response.AttendancePercentage = totalDaysHeld > 0 ? Math.Round((decimal)totalDaysAttended / totalDaysHeld * 100, 1) : 0;

            return response;
        }
    }
}
