using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class SocialLoginRequest
    {
        public string Provider { get; set; } // "Google" or "Apple"
        public string IdToken { get; set; }
        public string Role { get; set; } // "Tutor" or "Student" (for new users)
    }
}
