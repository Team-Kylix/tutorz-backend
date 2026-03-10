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

        public PaymentService(TutorzDbContext context)
        {
            _context = context;
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

            var payment = new ClassPayment
            {
                PaymentId = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClassId = request.ClassId,
                InstituteId = instituteId,
                Month = request.Month,
                Year = request.Year,
                AmountPaid = request.AmountPaid,
                Status = PaymentStatus.Paid.ToString(),
                PaidAt = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                Note = request.Note
            };

            await _context.ClassPayments.AddAsync(payment);
            await _context.SaveChangesAsync();

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
    }
}
