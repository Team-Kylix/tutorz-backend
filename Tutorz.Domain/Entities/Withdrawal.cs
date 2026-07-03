using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public enum PaymentMethod
    {
        OnHand,
        Online
    }

    /// <summary>
    /// Represents a withdrawal of funds by a Tutor from an Institute.
    /// </summary>
    public class Withdrawal
    {
        [Key]
        public Guid WithdrawalId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(30)]
        public string ReferenceId { get; set; } = string.Empty;

        [Required]
        public Guid InstituteId { get; set; }

        [Required]
        public Guid TutorId { get; set; }

        /// <summary>Optional class filter if the withdrawal was specific to a class.</summary>
        public Guid? ClassId { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        /// <summary>The available balance (sum of BaseFee or TuitionAmount) before this withdrawal</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        /// <summary>The amount being withdrawn</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal WithdrawalAmount { get; set; }

        /// <summary>Calculated as CurrentBalance - WithdrawalAmount</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>The PayHere charges deducted from the tutor's take-home if paid online</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PayoutCommission { get; set; }

        public DateTime WithdrawalAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; } = null!;

        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; } = null!;
    }
}
