using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class MonthlyUsageSummary
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        
        /// <summary>
        /// e.g. "Tutor" or "Institute"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string UserRole { get; set; } 
        
        /// <summary>
        /// 1 - 12 representing the calendar month
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// e.g. 2026
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// Aggregated cost of SMS based on SmsLogs.BillTo
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SmsCost { get; set; }
        
        /// <summary>
        /// Aggregated server cost based on Attendances (1.5 LKR per attendance record)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServerCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
