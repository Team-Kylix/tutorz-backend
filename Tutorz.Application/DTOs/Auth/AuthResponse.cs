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
        public string FirstName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
    }

    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public Guid? CurrentStudentId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
        public List<StudentProfileDto> Profiles { get; set; } = new();
    }
}
