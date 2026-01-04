using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class Institute
    {
        public Guid InstituteId { get; set; }
        public string RegistrationNumber { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public string InstituteName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string? Website { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
    }
}