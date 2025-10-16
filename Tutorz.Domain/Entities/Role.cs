using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class Role
    {
        public int RoleId { get; set; }
        public string Name { get; set; } // e.g., 'Admin', 'Tutor', 'Student' , Institution
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        DateTime UpdatedDate { get; set; }

    }
}
