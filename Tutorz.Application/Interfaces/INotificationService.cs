using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Notifications;

namespace Tutorz.Application.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Returns the latest 50 notifications for the given user (newest first).
        /// </summary>
        Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId);

        /// <summary>
        /// Marks a single notification as read. UserId is verified to prevent unauthorized access.
        /// </summary>
        Task MarkAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Marks ALL notifications for this user as read (single batch DB update).
        /// </summary>
        Task MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Creates a Notification record in the DB and immediately pushes it to the
        /// recipient via SignalR if they are currently connected.
        /// </summary>
        Task CreateAndPushAsync(Guid recipientUserId, string title, string message, string type, Guid? relatedId = null);
    }
}
