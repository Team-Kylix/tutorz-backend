using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class UserSequence
    {
        [Key]
        public string PrefixKey { get; set; } // e.g., "STU-25-6", "TUT-25-6"
        public int LastNumber { get; set; } // e.g., 84, 52
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
