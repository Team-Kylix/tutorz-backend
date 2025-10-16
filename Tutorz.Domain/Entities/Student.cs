using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class Student
    {
        public Guid StudentId { get; set; }
        public Guid UserId { get; set; } // Foreign Key to User
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String SchoolName { get; set; }
        public String Grade { get; set; }
        public String ParentName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }

    }
}
