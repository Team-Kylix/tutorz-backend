using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public enum PaymentStatus
    {
        Paid,
        Pending
    }

    /// <summary>
    /// Records a student's monthly class fee payment for a specific class.
    /// One record per (StudentId, ClassId, Month, Year) — enforced at the service layer.
    /// </summary>
    public class ClassPayment
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid ClassId { get; set; }

        [Required]
        public Guid InstituteId { get; set; }

        /// <summary>Calendar month (1–12)</summary>
        [Required]
        public int Month { get; set; }

        /// <summary>Calendar year (e.g., 2025)</summary>
        [Required]
        public int Year { get; set; }

        /// <summary>Actual amount paid (editable from the class default fee)</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        /// <summary>Payment status</summary>
        [Required]
        public string Status { get; set; } = PaymentStatus.Paid.ToString();

        /// <summary>Timestamp when the payment was recorded</summary>
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Optional note recorded by the institute staff</summary>
        public string? Note { get; set; }

        // --- Commission & Platform Fields ---
        // Snapshot of Class.InstituteCommissionRate at the time of payment (immutable audit trail)
        [Column(TypeName = "decimal(5,2)")]
        public decimal? InstituteCommissionPercentage { get; set; }

        /// <summary>Platform levy on the institute (1% of InstituteAmount). e.g. 2.50 LKR.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? InstituteCommission { get; set; }

        /// <summary>Platform levy on the tutor (1% of TuitionAmount). e.g. 7.50 LKR.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TutorCommission { get; set; }

        /// <summary>AmountPaid × (InstituteCommissionPercentage / 100). Institute's gross share.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? InstituteAmount { get; set; }

        /// <summary>AmountPaid − InstituteAmount. Tutor's gross share.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TuitionAmount { get; set; }

        /// <summary>InstituteAmount + TuitionAmount. Total platform revenue from this payment.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPlatformAmount { get; set; }

        // --- Online Payment Fields ---
        public string? ReferenceId { get; set; } // Our internal order ID
        public string? PayHerePaymentId { get; set; } // ID returned by PayHere
        public string? PaymentMethod { get; set; } // e.g. "CARD", "VISA"
        public string? Hash { get; set; } // Hash for verification

        // --- Navigation Properties ---
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; }
    }
}
