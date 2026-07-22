using System;

namespace Tutorz.Domain.Entities
{
    public class UserAnnouncementRead
    {
        public Guid UserId { get; set; }
        public Guid AnnouncementId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public User User { get; set; }
        public SystemAnnouncement Announcement { get; set; }
    }
}
