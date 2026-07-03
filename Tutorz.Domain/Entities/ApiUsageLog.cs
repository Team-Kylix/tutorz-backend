using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class ApiUsageLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(255)]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Method { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Purpose { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
