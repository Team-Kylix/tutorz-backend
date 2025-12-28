using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Student
{
    public class ClassSearchDto
    {
        public Guid Id { get; set; }
        public string Subject { get; set; }
        public string Grade { get; set; }
        public string TutorName { get; set; }
        public string TutorId { get; set; } 
        public string Bio { get; set; }
        public decimal Fee { get; set; }
        public string DayOfWeek { get; set; }
        public string StartTime { get; set; } 
        public string EndTime { get; set; }
        public string ClassType { get; set; }
        public string Status { get; set; }
    }
}
