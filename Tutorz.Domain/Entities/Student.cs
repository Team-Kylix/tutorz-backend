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
        [Key]
        public Guid StudentId { get; set; } = Guid.NewGuid();
        public string RegistrationNumber { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SchoolName { get; set; }
        public string Grade { get; set; }
        public string ParentName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsPrimary { get; set; } = false;
    }
}