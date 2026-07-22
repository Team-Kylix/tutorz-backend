using System;
using System.Collections.Generic;

namespace Tutorz.Domain.Entities
{
    public class SystemAnnouncement
    {
        public Guid AnnouncementId { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "SystemUpdate";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public ICollection<UserAnnouncementRead> UserReads { get; set; } = new List<UserAnnouncementRead>();
    }
}
