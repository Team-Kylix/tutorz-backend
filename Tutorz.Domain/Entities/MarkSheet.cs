using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class MarkSheet
    {
        [Key]
        public Guid MarkSheetId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        public Guid TutorId { get; set; }
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        public Guid ClassId { get; set; }
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public Guid? InstituteId { get; set; }
        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<MarkRecord> MarkRecords { get; set; } = new List<MarkRecord>();
        
        public bool IsDeleted { get; set; } = false;
    }
}
