using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Wallet
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid UserId { get; set; }

        /// <summary>null = Individual class wallet. A Guid = earnings from that Institute.</summary>
        public Guid? InstituteId { get; set; }

        /// <summary>True when this wallet tracks individual (non-institute) class earnings.</summary>
        public bool IsIndividual { get; set; } = false;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("InstituteId")]
        public virtual Institute? Institute { get; set; }
    }
}
