using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }

    public class CreateClassRequest
    {
        [Required] public string Subject { get; set; }
        [Required] public string Grade { get; set; }
        public string ClassName { get; set; }
        [Required] public string DayOfWeek { get; set; }
        [Required] public string StartTime { get; set; }
        [Required] public string EndTime { get; set; }
        public string HallName { get; set; }
        public decimal Fee { get; set; }
    }

    public class AddStudentRequest
    {
        [Required] public Guid ClassId { get; set; }
        [Required] public string StudentRegistrationNumber { get; set; }
    }
}
