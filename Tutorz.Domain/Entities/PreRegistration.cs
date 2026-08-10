using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tutorz.Domain.Entities
{
    public class PreRegistration
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CreatorId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CreatorRole { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PreAllocatedRegNo { get; set; } = string.Empty;

        public Guid PreAllocatedUserId { get; set; }

        public Guid PreAllocatedStudentId { get; set; }

        [MaxLength(20)]
        public string? MobileNumber { get; set; }

        public int Status { get; set; } // 0 = Available, 1 = InProgress, 2 = Completed

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
