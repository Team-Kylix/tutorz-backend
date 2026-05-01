using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public enum BillStatus
    {
        Unpaid,
        Paid,
        Overdue
    }

    /// <summary>
    /// Represents a monthly platform usage bill for an Institute or Tutor.
    /// Generated automatically on the 1st of each month (Sri Lanka time) for the previous month.
    /// One bill per user per month — enforced by unique index on (UserId, Month, Year).
    /// </summary>
    public class Bill
    {
        [Key]
        public Guid BillId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>"Institute" | "Tutor" | "Student"</summary>
        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = string.Empty;

        /// <summary>Human-readable bill reference e.g. TZ-2025-04-0001</summary>
        [MaxLength(30)]
        public string BillReference { get; set; } = string.Empty;

        /// <summary>Calendar month (1–12)</summary>
        [Required]
        public int Month { get; set; }

        /// <summary>Calendar year (e.g. 2025)</summary>
        [Required]
        public int Year { get; set; }

        /// <summary>"YYYY-MM" for fast lookup and display</summary>
        [Required]
        [MaxLength(7)]
        public string MonthYear { get; set; } = string.Empty;

        /// <summary>First moment of the billing period (LKT midnight)</summary>
        [Required]
        public DateTime BillStartDate { get; set; }

        /// <summary>Last moment of the billing period (LKT end-of-day)</summary>
        [Required]
        public DateTime BillEndDate { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // ─── Line Items (denormalised for PDF / audit) ───────────────────────

        /// <summary>Total API calls made this month</summary>
        public int ApiCallCount { get; set; }

        /// <summary>API call rate used at generation time (LKR per call)</summary>
        [Column(TypeName = "decimal(10,4)")]
        public decimal ApiCallRate { get; set; }

        /// <summary>ApiCallCount × ApiCallRate</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ApiUsageAmount { get; set; }

        /// <summary>Total SMS sent this month</summary>
        public int SmsSentCount { get; set; }

        /// <summary>SMS rate used at generation time (LKR per SMS)</summary>
        [Column(TypeName = "decimal(10,4)")]
        public decimal SmsRate { get; set; }

        /// <summary>SmsSentCount × SmsRate</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SmsAmount { get; set; }

        /// <summary>Sum of ClassPayments.TotalPlatformAmount for this user this month</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformCommissionAmount { get; set; }

        /// <summary>PayableAmount from the previous month's bill if it was Unpaid/Overdue</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousOverdueAmount { get; set; }

        /// <summary>ApiUsageAmount + SmsAmount + PlatformCommissionAmount + PreviousOverdueAmount</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        /// <summary>Tax rate snapshot used at generation time (e.g. 18.00 for 18% VAT)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxPercentage { get; set; }

        /// <summary>SubTotal × TaxPercentage / 100</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        /// <summary>SubTotal + TaxAmount — the amount the user must pay</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PayableAmount { get; set; }

        // ─── Status ──────────────────────────────────────────────────────────

        [Required]
        [MaxLength(10)]
        public string Status { get; set; } = BillStatus.Unpaid.ToString();

        public DateTime? PaidAt { get; set; }

        // ─── Navigation ──────────────────────────────────────────────────────

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
