using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Domain.Entities
{
    public class Province
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<District> Districts { get; set; }
    }
}
