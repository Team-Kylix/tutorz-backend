using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Institute
{
    public class InstituteProfileDto
    {
        public Guid InstituteId { get; set; }
        public string RegistrationNumber { get; set; }
        public string InstituteName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string Website { get; set; }
        public string Email { get; set; } // Fetched from User table
    }

    public class UpdateInstituteProfileDto
    {
        public string InstituteName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string Website { get; set; }
    }
}