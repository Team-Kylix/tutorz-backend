using System;

namespace Tutorz.Application.DTOs.Withdrawal
{
    public class MonthlyFeeRowDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Period { get; set; } = string.Empty;

        public decimal GrossCollected { get; set; }
        public decimal CommissionDeducted { get; set; }
        public decimal NetEarnings { get; set; }

        public Guid? TutorId { get; set; }
        public string? TutorName { get; set; }
        public Guid? InstituteId { get; set; }
        public string? InstituteName { get; set; }
    }
}
