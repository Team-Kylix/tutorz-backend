using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Tutor
{
    public class UpdateTutorProfileDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; } // Living in User table
        public string Bio { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
    }
}
