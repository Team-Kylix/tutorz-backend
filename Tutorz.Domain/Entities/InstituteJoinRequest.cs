using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tutorz.Domain.Enums;

namespace Tutorz.Domain.Entities
{
    public class InstituteJoinRequest
    {
        [Key]
        public Guid Id { get; set; }

        public Guid InstituteId { get; set; }
        [ForeignKey("InstituteId")]
        public virtual Institute Institute { get; set; }

        public Guid? StudentId { get; set; }
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        public Guid? TutorId { get; set; }
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
        public RequestInitiator InitiatedBy { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
