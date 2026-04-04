using System;

namespace Tutorz.Application.DTOs.Student
{
    public class StudentClassDto
    {
        public Guid ClassId { get; set; }
        public string Subject { get; set; }
        public string Grade { get; set; }
        public string ClassName { get; set; }
        public string TutorName { get; set; }
        public string InstituteName { get; set; }
        public string ClassType { get; set; }
        public string DayOfWeek { get; set; }
        public DateTime? Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public decimal Fee { get; set; }
        public string Status { get; set; } // "active" or "inactive" depending on Class.IsActive
        public DateTime? EnrolledAt { get; set; }
    }
}
