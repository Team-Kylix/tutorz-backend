using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tutorz.Application.DTOs.Admin
{
    public class UpdateAdminProfileDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        public IFormFile? ProfilePicture { get; set; }
    }
}
