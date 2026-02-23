using System;

namespace Tutorz.Domain.Entities
{
    public class InstituteStudent
    {
        public Guid InstituteId { get; set; }
        public virtual Institute Institute { get; set; }

        public Guid StudentId { get; set; }
        public virtual Student Student { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    }
}
