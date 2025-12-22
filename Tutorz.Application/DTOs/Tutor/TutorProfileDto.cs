using System;

namespace Tutorz.Application.DTOs.Tutor
{
    public class TutorProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }       // From User Table
        public string PhoneNumber { get; set; } // From User Table
        public string Bio { get; set; }         // From Tutor Table
        public string BankAccountNumber { get; set; } // From Tutor Table
        public string BankName { get; set; }          // From Tutor Table
        public string RegistrationNumber { get; set; }
    }
}