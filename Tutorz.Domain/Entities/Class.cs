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

        [Required]
        public string InstituteName { get; set; } 

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

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public Tutor Tutor { get; set; }
    }
}