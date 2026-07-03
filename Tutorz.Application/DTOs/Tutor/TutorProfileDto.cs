using System;

namespace Tutorz.Application.DTOs.Tutor
{
    public class TutorProfileDto
    {
        public Guid UserId { get; set; }
        public Guid TutorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }       // From User Table
        public string PhoneNumber { get; set; } // From User Table
        public string Bio { get; set; }         // From Tutor Table
        public string BankAccountNumber { get; set; } // From Tutor Table
        public string BankName { get; set; }          // From Tutor Table
        public int ExperienceYears { get; set; }
        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }
        public string RegistrationNumber { get; set; }
        public string? Address { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
    }
}