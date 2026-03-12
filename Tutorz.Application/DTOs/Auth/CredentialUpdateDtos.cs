using System;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.Auth
{
    public class RequestCredentialUpdateDto
    {
        [Required]
        public string NewIdentifier { get; set; } // Email or PhoneNumber
    }

    public class VerifyCredentialUpdateDto
    {
        [Required]
        public string NewIdentifier { get; set; }

        [Required]
        public string Otp { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        [MaxLength(10)]
        public string NewPassword { get; set; }
    }
}
