using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.System
{
    public class CreateAdminDto
    {
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        [RegularExpression(@"^07\d{8}$", ErrorMessage = "Invalid phone number format. Must be 10 digits starting with 07.")]
        public string PhoneNumber { get; set; } = null!;

        [EmailAddress]
        public string? Email { get; set; }
    }
}
