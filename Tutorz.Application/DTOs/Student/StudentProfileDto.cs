using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Student
{
    public class StudentProfileDto
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SchoolName { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string RegistrationNumber { get; set; }
        public string Email { get; set; }
    }

    public class UpdateStudentProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SchoolName { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public DateTime DateOfBirth { get; set; }
    }

}