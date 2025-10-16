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
        public Guid UserId { get; set; } // Foreign Key to User
        public String InstituteName { get; set; }
        public String Address { get; set; }
        public String ContactNumber { get; set; }
        public String Website { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
    }
}
