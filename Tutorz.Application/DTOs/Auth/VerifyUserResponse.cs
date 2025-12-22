using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class VerifyUserResponse
    {
        public bool Success { get; set; }
        public string PhoneNumber { get; set; }
    }
}
