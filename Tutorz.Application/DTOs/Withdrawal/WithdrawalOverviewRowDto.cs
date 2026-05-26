using System;

namespace Tutorz.Application.DTOs.Withdrawal
{
    /// <summary>
    /// One row in the Withdrawals overview table.
    /// Returned by GET /api/withdrawal/overview (tutor) and GET /api/withdrawal/overview-institute (institute).
    /// A row exists for every tutor-institute pairing that has at least one payment — even if no withdrawal
    /// has been processed yet (in which case WithdrawalAmount is null).
    /// </summary>
    public class WithdrawalOverviewRowDto
    {
        /// <summary>Reference of the most recent withdrawal, or null if none yet.</summary>
        public string? ReferenceId { get; set; }

        /// <summary>Scope description, e.g. "Test Institute · Physics Grade 11"</summary>
        public string DetailsPeriod { get; set; } = string.Empty;

        /// <summary>
        /// The withdrawal period: from the day after the last withdrawal (or earliest payment date)
        /// through today (UTC). Formatted as "dd MMM – dd MMM yyyy".
        /// </summary>
        public string WithdrawalPeriod { get; set; } = string.Empty;

        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd   { get; set; }

        /// <summary>Current available balance = sum(TuitionAmount) - sum(WithdrawalAmount)</summary>
        public decimal AvailableBalance { get; set; }

        /// <summary>Amount of the most recent withdrawal. Null if no withdrawal has been processed yet.</summary>
        public decimal? WithdrawalAmount { get; set; }

        /// <summary>Payment method of the last withdrawal. Null if none.</summary>
        public string? PaymentMethod { get; set; }

        /// <summary>WithdrawalId of the last withdrawal (used for PDF download). Null if none.</summary>
        public Guid? LastWithdrawalId { get; set; }

        // For institute-side rows
        public Guid? TutorId   { get; set; }
        public string TutorName { get; set; } = string.Empty;

        // For tutor-side rows
        public Guid? InstituteId   { get; set; }
        public string InstituteName { get; set; } = string.Empty;

        /// <summary>True if this row represents the current pending balance; False if it's a historical withdrawal.</summary>
        public bool IsPendingRow { get; set; }

        /// <summary>The exact date and time the withdrawal was processed (if historical).</summary>
        public DateTime? WithdrawalAt { get; set; }
    }
}
