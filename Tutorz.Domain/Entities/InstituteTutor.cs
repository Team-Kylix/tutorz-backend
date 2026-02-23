using System;

namespace Tutorz.Domain.Entities
{
    public class InstituteTutor
    {
        public Guid InstituteId { get; set; }
        public virtual Institute Institute { get; set; }

        public Guid TutorId { get; set; }
        public virtual Tutor Tutor { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
