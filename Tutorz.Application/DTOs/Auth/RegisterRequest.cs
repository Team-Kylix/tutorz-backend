
namespace Tutorz.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public int ExperienceYears { get; set; } = 0;
        public string? Address { get; set; }
        public string? InstituteName { get; set; }
        public string? SchoolName { get; set; }
        public string? Grade { get; set; }
        public string? ParentName { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}