using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class Institute
    {
        public Guid InstituteId { get; set; }
        public string RegistrationNumber { get; set; }
        public Guid UserId { get; set; } 
        public String InstituteName { get; set; }
        public String Address { get; set; }
        public String ContactNumber { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
    }
}
