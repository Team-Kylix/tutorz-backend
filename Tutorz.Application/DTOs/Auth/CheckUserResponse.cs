using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Auth
{
    public class CheckUserResponse
    {
        public bool Exists { get; set; }
        public string Message { get; set; }
        public Guid? UserId { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }
}
