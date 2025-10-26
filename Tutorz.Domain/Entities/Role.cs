using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class Role
    {
        public Guid RoleId { get; set; } 
        public string Name { get; set; } // e.g., 'Admin', 'Tutor', 'Student' , Institution
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}