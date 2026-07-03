using System;

namespace Tutorz.Application.DTOs.Withdrawal
{
    public class WithdrawalDto
    {
        public Guid WithdrawalId { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public Guid InstituteId { get; set; }
        public string InstituteName { get; set; } = string.Empty;
        public Guid TutorId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public Guid? ClassId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal WithdrawalAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal? PayoutCommission { get; set; }
        public DateTime WithdrawalAt { get; set; }
    }
}
