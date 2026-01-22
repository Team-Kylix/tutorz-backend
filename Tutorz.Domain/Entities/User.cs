using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public Guid RoleId { get; set; }
        public string? QrCodeUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Security / Reset logic
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public string? OtpCode { get; set; }
        public DateTime? OtpExpires { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual Tutor? Tutor { get; set; }
    }
}
