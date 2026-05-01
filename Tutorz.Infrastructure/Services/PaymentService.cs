using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Payment;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly TutorzDbContext _context;
        private readonly IBillService _billService;

        public PaymentService(TutorzDbContext context, IBillService billService)
        {
            _context = context;
            _billService = billService;
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<IEnumerable<MonthPaymentStatusDto>>> GetPaymentStatusAsync(
            Guid classId, Guid studentId, Guid instituteId)
        {
            // 1. Fetch the student's assignment date to this institute
            var assignment = await _context.InstituteStudents
                .AsNoTracking()
                .FirstOrDefaultAsync(is_ => is_.StudentId == studentId && is_.InstituteId == instituteId);

            // Fetch all payments for this student+class combination
            var payments = await _context.ClassPayments
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && p.ClassId == classId)
                .ToListAsync();

            var today = DateTime.UtcNow;
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            // 2. Determine the start date: Student's AssignedDate (rounded to start of month)
            // If No assignment found (shouldn't happen), default to 6 months ago.
            DateTime start = assignment != null 
                ? new DateTime(assignment.AssignedDate.Year, assignment.AssignedDate.Month, 1)
                : currentMonth.AddMonths(-6);

            // 3. Determine the end date: 3 months into the future
            var end = currentMonth.AddMonths(3);

            var statuses = new List<MonthPaymentStatusDto>();

            var pointer = start;
            while (pointer <= end)
            {
                var m = pointer.Month;
                var y = pointer.Year;

                // Priority 1: Check if already paid (this applies even to future months)
                var paid = payments.Any(p => p.Month == m && p.Year == y
                                             && p.Status == PaymentStatus.Paid.ToString());

                string status;
                if (paid)
                {
                    status = "Paid";
                }
                else if (pointer > currentMonth)
                {
                    status = "Future";
                }
                else
                {
                    status = "Unpaid";
                }

                statuses.Add(new MonthPaymentStatusDto
                {
                    Month = m,
                    Year = y,
                    Status = status
                });

                pointer = pointer.AddMonths(1);
            }

            return new ServiceResponse<IEnumerable<MonthPaymentStatusDto>>
            {
                Success = true,
                Data = statuses
            };
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<ClassPaymentDto>> RecordPaymentAsync(
            RecordPaymentRequest request, Guid instituteId)
        {
            // Duplicate guard
            var existing = await _context.ClassPayments
                .AnyAsync(p => p.StudentId == request.StudentId
                            && p.ClassId == request.ClassId
                            && p.Month == request.Month
                            && p.Year == request.Year);

            if (existing)
                return new ServiceResponse<ClassPaymentDto>
                {
                    Success = false,
                    Message = $"Payment for {new DateTime(request.Year, request.Month, 1):MMMM yyyy} has already been recorded."
                };

            // Validate amount
            if (request.AmountPaid <= 0)
                return new ServiceResponse<ClassPaymentDto>
                {
                    Success = false,
                    Message = "Amount paid must be greater than zero."
                };

            // Fetch class and student for denormalised response
            var cls = await _context.Classes
                .AsNoTracking()
                .Include(c => c.Tutor)
                .FirstOrDefaultAsync(c => c.ClassId == request.ClassId);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == request.StudentId);

            if (cls == null || student == null)
                return new ServiceResponse<ClassPaymentDto>
                {
                    Success = false,
                    Message = "Class or Student not found."
                };

            // Use the per-payment override if the caller supplied one (user changed it at payment time);
            // otherwise fall back to the rate stored on the class (seeded from institute default at creation).
            decimal instituteCommissionPercentage =
                request.InstituteCommissionPercentage.HasValue
                    ? request.InstituteCommissionPercentage.Value
                    : cls.InstituteCommissionRate;

            // --- Commission Calculation (Tutorz Billing Formula) ---
            // 1. Calculate how the class fee is split between the Institute and the Tutor
            decimal instituteAmount = Math.Round(request.AmountPaid * (instituteCommissionPercentage / 100m), 2);
            decimal tuitionAmount = request.AmountPaid - instituteAmount;

            // 2. Fetch platform commission rate from system config (defaults to 1% total)
            var configResponse = await _billService.GetBillingConfigAsync();
            decimal platformRate = (configResponse?.Data?.PlatformCommissionRate ?? 1.00m) / 100m;

            // 3. Calculate Platform Fees based on the actual earnings of each party
            // e.g. If Institute gets 15 LKR and Tutor gets 85 LKR, and platform rate is 1%:
            // → InstitutePlatformCommission = 15 * 0.01 = 0.15
            // → TutorPlatformCommission     = 85 * 0.01 = 0.85
            decimal instituteCommission = Math.Round(instituteAmount * platformRate, 2);
            decimal tutorCommission = Math.Round(tuitionAmount * platformRate, 2);
            decimal totalPlatformAmount = instituteCommission + tutorCommission;

            var payment = new ClassPayment
            {
                PaymentId = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                // Null for tutor own-place classes (no institute)
                InstituteId = instituteId == Guid.Empty ? null : instituteId,
                Month = request.Month,
                Year = request.Year,
                AmountPaid = request.AmountPaid,
                Status = PaymentStatus.Paid.ToString(),
                PaidAt = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                Note = request.Note,
                InstituteCommissionPercentage = instituteCommissionPercentage,
                InstituteCommission = instituteCommission,
                TutorCommission = tutorCommission,
                InstituteAmount = instituteAmount,
                TuitionAmount = tuitionAmount,
                TotalPlatformAmount = totalPlatformAmount
            };

            await _context.ClassPayments.AddAsync(payment);
            await _context.SaveChangesAsync();

            // Fire real-time bill update incrementally
            // Only pass a real instituteId — Guid.Empty means own-place class (no institute bill)
            await _billService.IncrementPlatformCommissionAsync(
                instituteId == Guid.Empty ? Guid.Empty : instituteId,
                cls.TutorId, instituteCommission, tutorCommission,
                request.Month, request.Year);

            var dto = new ClassPaymentDto
            {
                PaymentId = payment.PaymentId,
                StudentId = payment.StudentId,
                ClassId = payment.ClassId,
                InstituteId = payment.InstituteId,
                Month = payment.Month,
                Year = payment.Year,
                AmountPaid = payment.AmountPaid,
                Status = payment.Status,
                PaidAt = payment.PaidAt,
                Note = payment.Note,
                StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                ClassName = cls.ClassName ?? cls.Subject,
                Subject = cls.Subject
            };

            return new ServiceResponse<ClassPaymentDto>
            {
                Success = true,
                Data = dto,
                Message = "Payment recorded successfully."
            };
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FinancialHistoryResponseDto>> GetClassPaymentHistoryAsync(
            Guid instituteId, Guid? tutorId, Guid? classId, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            var query = _context.ClassPayments
                .AsNoTracking()
                .Include(p => p.Student)
                .ThenInclude(s => s.User)
                .Include(p => p.Class)
                .Where(p => p.InstituteId == instituteId);

            if (classId.HasValue && classId.Value != Guid.Empty)
            {
                query = query.Where(p => p.ClassId == classId.Value);
            }
            else if (tutorId.HasValue && tutorId.Value != Guid.Empty)
            {
                query = query.Where(p => p.Class != null && p.Class.TutorId == tutorId.Value);
            }
            else
            {
                return new ServiceResponse<FinancialHistoryResponseDto> 
                { 
                    Success = false, 
                    Message = "Please select a tutor or class." 
                };
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var lowerQuery = searchQuery.ToLower();
                query = query.Where(p =>
                    (p.Student.FirstName != null && p.Student.FirstName.ToLower().Contains(lowerQuery)) ||
                    (p.Student.LastName != null && p.Student.LastName.ToLower().Contains(lowerQuery)) ||
                    (p.Student.RegistrationNumber != null && p.Student.RegistrationNumber.ToLower().Contains(lowerQuery)) ||
                    (p.Student.User != null && p.Student.User.PhoneNumber != null && p.Student.User.PhoneNumber.Contains(lowerQuery)));
            }

            // Summary Stats before pagination
            decimal totalReceived = await query.SumAsync(p => p.AmountPaid);
            int totalStudents = await query.Select(p => p.StudentId).Distinct().CountAsync();
            int totalRecords = await query.CountAsync();

            // Fetch commission
            var institute = await _context.Institutes.AsNoTracking().FirstOrDefaultAsync(i => i.InstituteId == instituteId);
            decimal commission = institute?.CommissionPercentage ?? 0;
            decimal instituteShare = totalReceived * (commission / 100);
            decimal teacherShare = totalReceived - instituteShare;

            var payments = await query
                .OrderByDescending(p => p.PaidAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ClassPaymentHistoryDto
                {
                    PaymentId = p.PaymentId,
                    StudentId = p.StudentId,
                    StudentName = $"{p.Student.FirstName} {p.Student.LastName}".Trim(),
                    RegistrationNumber = p.Student.RegistrationNumber,
                    MobileNumber = p.Student.User != null ? p.Student.User.PhoneNumber ?? "" : "",
                    MonthYear = new DateTime(p.Year, p.Month, 1).ToString("MMM yyyy"),
                    AmountPaid = p.AmountPaid,
                    PaidAt = p.PaidAt,
                    Status = p.Status,
                    Note = p.Note
                })
                .ToListAsync();

            var paginatedResult = new PaginatedResultDto<ClassPaymentHistoryDto>
            {
                Items = payments,
                TotalCount = totalRecords,
                Page = page,
                PageSize = pageSize
            };

            var responseDto = new FinancialHistoryResponseDto
            {
                PaginatedPayments = paginatedResult,
                TotalReceived = totalReceived,
                TeacherShare = teacherShare,
                InstituteShare = instituteShare,
                TotalStudents = totalStudents
            };

            return new ServiceResponse<FinancialHistoryResponseDto>
            {
                Success = true,
                Data = responseDto
            };
        }
    }
}
