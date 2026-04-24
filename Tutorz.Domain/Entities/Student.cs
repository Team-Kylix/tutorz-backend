using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class Student
    {
        public Guid StudentId { get; set; }

       
        public Guid UserId { get; set; }
       
        public virtual User? User { get; set; }

      
        public string RegistrationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

      
        public string SchoolName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        
        public string? Address { get; set; }

        public bool IsPrimary { get; set; } = false;

        public string? ProfileImageUrlSmall { get; set; }
        public string? ProfileImageUrlLarge { get; set; }

        // --- Card / PayHere Token (Students are payers, not payees — card only) ---
        public string? PayHereToken { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? CardholderName { get; set; }
        public string? CardExpiry { get; set; }  // MMYY format e.g. "0128"

        public virtual ICollection<InstituteStudent> InstituteStudents { get; set; } = new List<InstituteStudent>();
    }
}