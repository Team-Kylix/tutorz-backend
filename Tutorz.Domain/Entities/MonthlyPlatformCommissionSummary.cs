using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class MonthlyPlatformCommissionSummary
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid ClassId { get; set; }
        public Guid TutorId { get; set; }
        public Guid? InstituteId { get; set; } // Null for individual classes without an institute
        
        public int Month { get; set; }
        public int Year { get; set; } 
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformInstituteCommission { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformTutorCommission { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PlatformAllCommission { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }
        
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }
        
        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; }
    }
}
