using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tutorz.Domain.Enums;

namespace Tutorz.Domain.Entities
{
    public class WalletTransaction
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid WalletId { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        public TransactionType Type { get; set; }
        
        [MaxLength(255)]
        public string? ReferenceId { get; set; } 
        
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>Which Institute this transaction relates to. Null = individual class withdrawal.</summary>
        public Guid? InstituteId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; }

        [ForeignKey("InstituteId")]
        public virtual Institute? Institute { get; set; }
    }
}
