using System;

namespace Tutorz.Application.DTOs.Admin
{
    public class AdminProfileDto
    {
        public Guid AdminId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
        public string Role { get; set; } = "Admin";
        public DateTime CreatedDate { get; set; }
    }
}
