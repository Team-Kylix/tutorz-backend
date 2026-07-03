using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public enum ClassType
    {
        Class,      
        Seminar,    
        Workshop,   
        Course      
    }

    public class Class
    {
        [Key]
        public Guid ClassId { get; set; }
        public Guid TutorId { get; set; }

        public Guid? InstituteId { get; set; } 

        public string ClassType { get; set; } 

        [Required]
        public string Subject { get; set; }

        public string Grade { get; set; } 
        public string ClassName { get; set; } 

        public string DayOfWeek { get; set; } 

        public DateTime? Date { get; set; } 

        [Required]
        public string StartTime { get; set; } 
        [Required]
        public string EndTime { get; set; }  

        public string HallName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; }

        /// <summary>
        /// Percentage of class fee the institute keeps (e.g. 25.00 = 25%).
        /// Independent tutors set this to 0. Snapshotted into ClassPayment at payment time.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal InstituteCommissionRate { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; }
    
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}