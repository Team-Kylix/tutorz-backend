using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class MonthlyBill
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(6)]
        public string BillNumber { get; set; }
        
        public Guid UserId { get; set; }
        
        public int Month { get; set; }
        public int Year { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BillAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DueAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }
        
        public bool IsPaid { get; set; }
        
        public bool IsIndividual { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
