using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Application.DTOs.Common;
using Tutorz.Infrastructure.Data;


namespace Tutorz.Infrastructure.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<PaginatedResultDto<ClassSearchDto>> SearchClassesAsync(string? grade, string? searchTerm, Guid? studentId = null, int? provinceId = null, int? districtId = null, int? cityId = null, int page = 1, int pageSize = 10)
        {
            var db = _context as TutorzDbContext;

            var query = db.Classes
                .Include(c => c.Tutor)
                .Include(c => c.Institute)
                    .ThenInclude(i => i.User)
                        .ThenInclude(u => u.City)
                .Where(c => c.IsActive && !c.IsDeleted);

            // Filter by Grade
            if (!string.IsNullOrEmpty(grade))
            {
                query = query.Where(c => c.Grade == grade);
            }

            // Filter by Province
            if (provinceId.HasValue)
            {
                query = query.Where(c => c.Institute != null && 
                                       c.Institute.User != null && 
                                       c.Institute.User.City != null && 
                                       c.Institute.User.City.District != null &&
                                       c.Institute.User.City.District.ProvinceId == provinceId.Value);
            }

            // Filter by District
            if (districtId.HasValue)
            {
                query = query.Where(c => c.Institute != null && 
                                       c.Institute.User != null && 
                                       c.Institute.User.City != null && 
                                       c.Institute.User.City.DistrictId == districtId.Value);
            }

            // Filter by City (Town)
            if (cityId.HasValue)
            {
                query = query.Where(c => c.Institute != null && 
                                       c.Institute.User != null && 
                                       c.Institute.User.CityId == cityId.Value);
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

            var totalCount = await query.CountAsync();

            // Select into DTO and paginate
            var items = await query
                .OrderBy(c => c.CreatedDate) // Ensure deterministic order for paging
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClassSearchDto
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
                    Status = c.IsActive ? "Active" : "Inactive",
                    StudentCount = c.Enrollments.Count(e => e.Status == EnrollmentStatus.Approved),
                    EnrollmentStatus = studentId.HasValue 
                        ? c.Enrollments.Where(e => e.StudentId == studentId.Value)
                                    .Select(e => e.Status.ToString())
                                    .FirstOrDefault() 
                        : null,
                    TutorImageUrl = c.Tutor.ProfileImageUrlLarge,
                    InstituteName = c.Institute != null ? c.Institute.InstituteName : null,
                    HallName = c.HallName
                }).ToListAsync();

            return new PaginatedResultDto<ClassSearchDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
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
                        .ThenInclude(t => t.User)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Institute)
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Approved)
                .Select(e => new StudentClassDto
                {
                    ClassId = e.ClassId,
                    Subject = e.Class.Subject,
                    Grade = e.Class.Grade,
                    ClassName = e.Class.ClassName,
                    TutorId = e.Class.Tutor.TutorId,
                    TutorName = e.Class.Tutor.FirstName + " " + e.Class.Tutor.LastName,
                    TutorRegistrationNumber = e.Class.Tutor.RegistrationNumber,
                    TutorPhoneNumber = e.Class.Tutor.User.PhoneNumber,
                    TutorProfileImageUrlSmall = e.Class.Tutor.ProfileImageUrlSmall,
                    InstituteName = e.Class.Institute != null ? e.Class.Institute.InstituteName : null,
                    ClassType = e.Class.ClassType,
                    DayOfWeek = e.Class.DayOfWeek,
                    Date = e.Class.Date,
                    StartTime = e.Class.StartTime,
                    EndTime = e.Class.EndTime,
                    HallName = e.Class.HallName,
                    Fee = e.Class.Fee,
                    Status = e.Class.IsActive ? "active" : "inactive",
                    EnrolledAt = e.EnrolledAt,
                    StudentCount = e.Class.Enrollments.Count(en => en.Status == EnrollmentStatus.Approved)
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
                    AttendanceRecord = new Dictionary<DateTime, bool>(),
                    ClassConductedDates = classConductedDates  // dates the class was actually held
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

        public async Task<StudentPaymentHistoryResponseDto> GetStudentPaymentHistoryAsync(Guid studentId, Guid? tutorId, Guid? classId, string? monthYear, int page, int pageSize)
        {
            var db = _context as TutorzDbContext;

            // ── 1. Build base payment query ───────────────────────────────────
            var paymentQuery = db.ClassPayments
                .Include(p => p.Class)
                    .ThenInclude(c => c.Tutor)
                .Where(p => p.StudentId == studentId)
                .AsQueryable();

            if (tutorId.HasValue)
                paymentQuery = paymentQuery.Where(p => p.Class.TutorId == tutorId.Value);

            if (classId.HasValue)
                paymentQuery = paymentQuery.Where(p => p.ClassId == classId.Value);

            if (!string.IsNullOrEmpty(monthYear) && DateTime.TryParse(monthYear, out DateTime parsedDate))
                paymentQuery = paymentQuery.Where(p => p.Month == parsedDate.Month && p.Year == parsedDate.Year);

            // ── 2. Aggregate stats from existing payments ─────────────────────
            decimal totalPaid = await paymentQuery.SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            // ── 3. Paid rows ──────────────────────────────────────────────────
            var paidRows = await paymentQuery
                .OrderByDescending(p => p.PaidAt)
                .Select(p => new StudentPaymentHistoryDto
                {
                    PaymentId   = p.PaymentId,
                    ClassId     = p.ClassId,
                    TutorId     = p.Class.TutorId,
                    TutorName   = p.Class.Tutor.FirstName + " " + p.Class.Tutor.LastName,
                    Subject     = p.Class.Subject,
                    ClassName   = p.Class.ClassName,
                    Month       = p.Month,
                    Year        = p.Year,
                    MonthYear   = new DateTime(p.Year, p.Month, 1).ToString("MMM yyyy"),
                    ClassFee    = p.Class.Fee,
                    AmountPaid  = p.AmountPaid,
                    PaidAt      = p.PaidAt,
                    Status      = p.Status,
                    Note        = p.Note
                })
                .ToListAsync();

            // ── 4. Due rows: class-months with attendance but no ClassPayment ─
            // Get all approved enrollments for this student
            var enrollmentsQuery = db.Enrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c.Tutor)
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Approved);

            if (tutorId.HasValue)
                enrollmentsQuery = enrollmentsQuery.Where(e => e.Class.TutorId == tutorId.Value);
            if (classId.HasValue)
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassId == classId.Value);

            var enrollments = await enrollmentsQuery.ToListAsync();
            decimal totalClassFees = enrollments.Sum(e => e.Class.Fee);

            // For each enrollment, find months with attendance but no payment
            var dueRows = new List<StudentPaymentHistoryDto>();

            // Set of (classId, month, year) already paid – to exclude from due rows
            var paidKeys = paidRows.Select(r => (r.ClassId, r.Month, r.Year)).ToHashSet();

            foreach (var enrollment in enrollments)
            {
                var cls = enrollment.Class;

                // Get distinct months where this student had attendance for this class
                var attendedMonths = await db.Attendances
                    .Where(a => a.ClassId == cls.ClassId && a.StudentId == studentId && a.IsPresent)
                    .Select(a => new { a.Date.Month, a.Date.Year })
                    .Distinct()
                    .ToListAsync();

                foreach (var am in attendedMonths)
                {
                    // Skip if already paid or if a month filter is applied and doesn't match
                    if (paidKeys.Contains((cls.ClassId, am.Month, am.Year))) continue;
                    if (!string.IsNullOrEmpty(monthYear) && DateTime.TryParse(monthYear, out DateTime pDate))
                    {
                        if (am.Month != pDate.Month || am.Year != pDate.Year) continue;
                    }

                    dueRows.Add(new StudentPaymentHistoryDto
                    {
                        PaymentId  = Guid.Empty, // No payment yet
                        ClassId    = cls.ClassId,
                        TutorId    = cls.TutorId,
                        TutorName  = $"{cls.Tutor.FirstName} {cls.Tutor.LastName}",
                        Subject    = cls.Subject,
                        ClassName  = cls.ClassName,
                        Month      = am.Month,
                        Year       = am.Year,
                        MonthYear  = new DateTime(am.Year, am.Month, 1).ToString("MMM yyyy"),
                        ClassFee   = cls.Fee,
                        AmountPaid = 0,
                        PaidAt     = null,
                        Status     = "Due",
                        Note       = null
                    });
                }
            }

            // ── 5. Merge: due rows first (newest month first), then paid rows ─
            var dueSorted   = dueRows.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToList();
            var allRows     = dueSorted.Concat(paidRows).ToList();
            int totalCount  = allRows.Count;

            // Apply pagination after merging
            var pagedItems = allRows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // ── 6. Compute due amount ─────────────────────────────────────────
            decimal totalDue = dueRows.Sum(r => r.ClassFee);

            return new StudentPaymentHistoryResponseDto
            {
                TotalAmountPaid = totalPaid,
                TotalClassFees  = totalClassFees,
                TotalDueAmount  = totalDue,
                PaginatedPayments = new Tutorz.Application.DTOs.Common.PaginatedResultDto<StudentPaymentHistoryDto>
                {
                    Items      = pagedItems,
                    TotalCount = totalCount,
                    Page       = page,
                    PageSize   = pageSize
                }
            };
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByClassAsync(Guid classId)
        {
            var db = _context as TutorzDbContext;
            return await db.Enrollments
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Where(e => e.ClassId == classId)
                .ToListAsync();
        }

        public async Task<PaginatedResultDto<StudentProfileDto>> GetAllStudentsAsync(string? searchQuery, int page, int pageSize)
        {
            var db = _context as TutorzDbContext;

            var query = db.Students
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string term = searchQuery.ToLower();
                query = query.Where(s =>
                    s.FirstName.ToLower().Contains(term) ||
                    s.LastName.ToLower().Contains(term) ||
                    s.RegistrationNumber.ToLower().Contains(term) ||
                    (s.User != null && s.User.PhoneNumber.Contains(term)) ||
                    (s.User != null && s.User.Email != null && s.User.Email.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(s => s.User != null ? s.User.CreatedDate : DateTime.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StudentProfileDto
                {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    RegistrationNumber = s.RegistrationNumber,
                    Grade = s.Grade,
                    Email = s.User != null ? s.User.Email : "",
                    PhoneNumber = s.User != null ? s.User.PhoneNumber : "",
                    ProfileImageUrlSmall = s.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = s.ProfileImageUrlLarge
                }).ToListAsync();

            return new PaginatedResultDto<StudentProfileDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
