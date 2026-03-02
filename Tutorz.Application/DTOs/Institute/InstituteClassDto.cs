using System;

namespace Tutorz.Application.DTOs.Institute
{
    public class InstituteClassDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; }
        public string ClassType { get; set; }
        public string Grade { get; set; }
        public bool IsActive { get; set; }
        public Guid TutorId { get; set; }
        public string TutorName { get; set; }
        public string Subject { get; set; }
        public string DayOfWeek { get; set; }
        public DateTime? Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string HallName { get; set; }
        public decimal Fee { get; set; }
        public int StudentRegisteredCount { get; set; }
    }
}
