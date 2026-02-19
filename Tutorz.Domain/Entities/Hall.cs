using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Hall
    {
        [Key]
        public Guid HallId { get; set; }

        public Guid InstituteId { get; set; }
        [ForeignKey("InstituteId")]
        public Institute Institute { get; set; }

        [MaxLength(200)]
        public string? HallCode { get; set; } // Generated ID: HALL(InstituteID)HALL(Name)

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // "Hall A", "Room 101"

        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
