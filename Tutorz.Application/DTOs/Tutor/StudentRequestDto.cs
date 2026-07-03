using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Tutor
{
    public class StudentRequestDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid StudentId { get; set; }
        public string Name { get; set; }
        public string RegNo { get; set; }
        public string Grade { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string TargetClass { get; set; }
        public string ClassType { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
