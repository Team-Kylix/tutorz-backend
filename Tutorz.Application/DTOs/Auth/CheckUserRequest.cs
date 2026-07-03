using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class CheckUserRequest
    {
        public string? Identifier { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
