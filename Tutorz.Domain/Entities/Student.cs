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

        
        public bool IsPrimary { get; set; } = false;
    }
}