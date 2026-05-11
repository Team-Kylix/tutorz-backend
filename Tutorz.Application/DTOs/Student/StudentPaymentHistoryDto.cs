using System;
using System.Collections.Generic;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Application.DTOs.Student
{
    public class StudentPaymentHistoryDto
    {
        public Guid PaymentId { get; set; }
        public Guid ClassId { get; set; }
        public Guid TutorId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MonthYear { get; set; } = string.Empty; // e.g., "April 2026"
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal ClassFee { get; set; }   // Raw class fee (no gateway charges)
        public decimal AmountPaid { get; set; } // Actual amount paid (may include gateway charges)
        public DateTime? PaidAt { get; set; }   // Nullable – Due rows have no paid date
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class StudentPaymentHistoryResponseDto
    {
        public PaginatedResultDto<StudentPaymentHistoryDto> PaginatedPayments { get; set; } = new();
        public decimal TotalAmountPaid { get; set; }

        /// <summary>
        /// Total fee owed for the current calendar month across ALL enrolled classes.
        /// </summary>
        public decimal TotalDueAmount { get; set; }

        /// <summary>
        /// Total enrolled class fees (for the selected month or overall).
        /// </summary>
        public decimal TotalClassFees { get; set; }
    }
}
