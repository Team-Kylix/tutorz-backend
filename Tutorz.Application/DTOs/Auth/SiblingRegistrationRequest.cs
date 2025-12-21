using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class SiblingRegistrationRequest
    {
        [Required]
        public string Identifier { get; set; } 

        [Required]
        public string VerificationToken { get; set; } 

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string SchoolName { get; set; }

        [Required]
        public string Grade { get; set; }

        public string? ParentName { get; set; } 

        [Required]
        public DateTime DateOfBirth { get; set; }
    }
}
