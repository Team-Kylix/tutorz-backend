using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Attendance
    {
        [Key]
        public Guid Id { get; set; }

        public Guid StudentId { get; set; }
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        public Guid ClassId { get; set; }
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public Guid InstituteId { get; set; }
        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; }

        [Required]
        public DateTime Date { get; set; } // The date the attendance is for

        public bool IsPresent { get; set; } = true;

        public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
    }
}
