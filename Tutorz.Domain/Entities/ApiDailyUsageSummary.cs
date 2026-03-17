using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class ApiDailyUsageSummary
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int TotalCalls { get; set; }
    }
}
