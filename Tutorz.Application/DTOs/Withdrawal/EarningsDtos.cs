using System;

namespace Tutorz.Application.DTOs.Withdrawal
{
    public class EarningsSummaryDto
    {
        public Guid Id { get; set; }
        public string ReferenceId { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string Period { get; set; } = string.Empty;

        public Guid? InstituteId { get; set; }
        public string? InstituteName { get; set; }

        public Guid? TutorId { get; set; }
        public string? TutorName { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal PlatformCommission { get; set; }
        public decimal InstituteCommission { get; set; }
        public decimal SmsDeduction { get; set; }
        public decimal ServerDeduction { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class WalletBalanceDto
    {
        public Guid WalletId { get; set; }
        public decimal Balance { get; set; }
        public bool IsIndividual { get; set; }
        public Guid? InstituteId { get; set; }
        public string? InstituteName { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class WithdrawDto
    {
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        /// <summary>OnHand or Online</summary>
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CalculateEarningsDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }

    public class CalculateInstituteEarningsDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
