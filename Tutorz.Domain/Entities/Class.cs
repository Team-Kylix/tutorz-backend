using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Class
    {
        [Key]
        public Guid ClassId { get; set; }
        public Guid TutorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Subject { get; set; }

        [Required]
        [MaxLength(50)]
        public string Grade { get; set; }

        [MaxLength(100)]
        public string ClassName { get; set; } // Optional custom name

        public string DayOfWeek { get; set; } // e.g., "Monday"
        public string StartTime { get; set; } // e.g., "14:00"
        public string EndTime { get; set; }   // e.g., "16:00"
        public string HallName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public Tutor Tutor { get; set; }
        // Depending on your architecture, you might have a joining table 'ClassStudent'
        // For simplicity here, assuming a collection or managed via a separate repository method
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
