using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Domain.Entities
{
    public enum EnrollmentStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Enrollment
    {
        [Key]
        public Guid Id { get; set; }

        public Guid StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }

        public Guid ClassId { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EnrolledAt { get; set; }
    }
}
