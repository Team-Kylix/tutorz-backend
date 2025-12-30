using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class StudentProfileDto
    {
        public Guid StudentId { get; set; }
        public string FirstName { get; set; }
        public string Grade { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public Guid? CurrentStudentId { get; set; } 
        public string Email { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RegistrationNumber { get; set; }
        public List<StudentProfileDto> Profiles { get; set; } = new();
    }
}
