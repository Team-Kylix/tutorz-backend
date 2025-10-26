// In Tutorz.Application.DTOs.Auth.RegisterRequest.cs
namespace Tutorz.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        // From the original form
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
    }
}