using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.Tutor
{
    public class ClassDto
    {
        public Guid ClassId { get; set; }
        public string Subject { get; set; }
        public string Grade { get; set; }
        public string ClassName { get; set; }
        public string DayOfWeek { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string HallName { get; set; }
        public decimal Fee { get; set; }
        public int StudentCount { get; set; }
        public bool IsActive { get; set; }
        public Guid? InstituteId { get; set; }
        public string InstituteName { get; set; }
        public string ClassType { get; set; }
        public DateTime? Date { get; set; }

        /// <summary>Snapshot of the institute commission rate at the time of class creation.</summary>
        public decimal InstituteCommissionRate { get; set; }
    }

    public class CreateClassRequest
    {
        public Guid? InstituteId { get; set; }
        public Guid? TutorId { get; set; }

        [Required]
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
        public decimal Fee { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional override of the institute commission rate for this class.
        /// When null, the institute's default CommissionPercentage is used.
        /// </summary>
        public decimal? InstituteCommissionRate { get; set; }
    }

    public class AddStudentRequest
    {
        [Required] public Guid ClassId { get; set; }
        [Required] public string StudentRegistrationNumber { get; set; }
    }
}