using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    /// <summary>
    /// Monthly net earnings record for a Tutor (individual or per-Institute) or an Institute.
    /// One row per (TutorId, InstituteId, Month, Year) combination.
    /// - InstituteId = null, TutorId = X  -> Tutor's individual class earnings
    /// - InstituteId = S,    TutorId = X  -> Tutor X's earnings from Institute S
    /// - InstituteId = S,    TutorId = null -> Institute S's own earnings
    /// </summary>
    public class EarningsSummary
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(40)]
        public string ReferenceId { get; set; } = string.Empty;

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        /// <summary>Null = Individual row; otherwise the Institute this row belongs to.</summary>
        public Guid? InstituteId { get; set; }

        /// <summary>Null = Institute-only row; otherwise the Tutor this row belongs to.</summary>
        public Guid? TutorId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformCommission { get; set; }

        /// <summary>Institute's revenue share deducted. 0 for individual rows.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal InstituteCommission { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SmsDeduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServerDeduction { get; set; }

        /// <summary>Net = GrossAmount - PlatformCommission - InstituteCommission - SmsDeduction - ServerDeduction.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("InstituteId")]
        public virtual Institute? Institute { get; set; }

        [ForeignKey("TutorId")]
        public virtual Tutor? Tutor { get; set; }
    }
}
