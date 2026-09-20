using System;
using System.Collections.Generic;

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

    public class EarningsPdfDto
    {
        public string ReferenceId { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string HeaderTitle { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public List<EarningsPdfClassRowDto> Classes { get; set; } = new();

        public int SmsCount { get; set; }
        public decimal TotalSmsCost { get; set; }
        public decimal TotalServerCost { get; set; }

        public decimal TotalGross { get; set; }
        public decimal TotalInstituteCommission { get; set; }
        public decimal TotalPlatformCommission { get; set; }
        public decimal FinalNetAmount { get; set; }
    }

    public class EarningsPdfClassRowDto
    {
        public string ClassName { get; set; } = string.Empty;
        public int PaymentsCount { get; set; }
        /// <summary>Full class fee (BaseFee total). For institute classes this is the full student fee before any split.</summary>
        public decimal GrossFees { get; set; }
        /// <summary>Institute's share cut from GrossFees (InstituteAmount). Only populated for tutor+institute rows.</summary>
        public decimal InstituteCut { get; set; }
        /// <summary>Platform's 1% commission on tutor's share (TutorCommission) or on institute share (InstituteCommission).</summary>
        public decimal PlatformCommission { get; set; }
        public int AttendanceCount { get; set; }
        public decimal ServerCost { get; set; }
        public decimal NetForClass { get; set; }
    }
}
