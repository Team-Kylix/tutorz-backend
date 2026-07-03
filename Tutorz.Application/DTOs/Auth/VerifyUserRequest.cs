using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class VerifyUserRequest
    {
        [Required]
        public string Identifier { get; set; } 

        [Required]
        public string Otp { get; set; } 
    }
}
