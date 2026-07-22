using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        /// <summary>
        /// Returns the latest notifications for a user, ordered by CreatedAt descending.
        /// </summary>
        Task<IEnumerable<Notification>> GetLatestForUserAsync(Guid userId, int take = 50);

        /// <summary>
        /// Mark all unread notifications for a user as read.
        /// </summary>
        Task MarkAllAsReadAsync(Guid userId);
        Task<IEnumerable<SystemAnnouncement>> GetActiveAnnouncementsAsync(DateTime joinedAt, DateTime cutoff);
        Task<IEnumerable<Guid>> GetReadAnnouncementIdsAsync(Guid userId);
        Task MarkAnnouncementAsReadAsync(Guid userId, Guid announcementId);
        Task MarkAllAnnouncementsAsReadAsync(Guid userId, IEnumerable<Guid> announcementIds);
    }
}
