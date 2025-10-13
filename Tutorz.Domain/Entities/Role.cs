using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    internal class Role
    {
        public int RoleId { get; set; }
        public string Name { get; set; } // e.g., 'Admin', 'Tutor', 'Student'
    }
}
