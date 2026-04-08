using System;

namespace Tutorz.Domain.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The UserId of the institute that should receive this notification.
        /// </summary>
        public Guid UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Notification category: "StudentRegistration", "TutorRegistration", etc.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: links back to the StudentId or TutorId that triggered this notification.
        /// </summary>
        public Guid? RelatedId { get; set; }

        // --- Navigation ---
        public User User { get; set; } = null!;
    }
}
