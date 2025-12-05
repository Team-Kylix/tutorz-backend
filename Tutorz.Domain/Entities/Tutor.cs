using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class Tutor
    {
        public Guid TutorId { get; set; }
        public Guid UserId { get; set; } // Foreign Key to User
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String Bio { get; set; }
        public int ExperienceYears { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
    }
}
