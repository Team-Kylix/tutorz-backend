using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;
using Tutorz.Application.DTOs.Notifications;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly INotificationPusher _notificationPusher;
        public NotificationService(
            INotificationRepository notificationRepo,
            INotificationPusher notificationPusher)
        {
            _notificationRepo = notificationRepo;
            _notificationPusher = notificationPusher;
        }

        public async Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId)
        {
            // Note: In a real system, you'd fetch the user's Join Date from a UserRepository.
            // For announcements, we can safely use a 30-day window.
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var notifications = await _notificationRepo.GetLatestForUserAsync(userId);
            var dtos = notifications.Select(MapToDto).ToList();

            var announcements = await _notificationRepo.GetActiveAnnouncementsAsync(thirtyDaysAgo, thirtyDaysAgo);
            
            if (announcements.Any())
            {
                var readIds = await _notificationRepo.GetReadAnnouncementIdsAsync(userId);
                
                foreach (var announcement in announcements)
                {
                    dtos.Add(new NotificationDto
                    {
                        NotificationId = announcement.AnnouncementId,
                        Title = announcement.Title,
                        Message = announcement.Message,
                        Type = announcement.Type,
                        IsRead = readIds.Contains(announcement.AnnouncementId),
                        CreatedAt = announcement.CreatedAt,
                        RelatedId = null
                    });
                }
            }

            return dtos.OrderByDescending(d => d.CreatedAt).Take(50);
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            // It could be a regular notification or a system announcement.
            // We can just try marking it as an announcement first. 
            // The repo method will do nothing if it's not an announcement.
            await _notificationRepo.MarkAnnouncementAsReadAsync(userId, notificationId);

            // Fetch and verify ownership for regular notifications
            var notification = await _notificationRepo.GetAsync(
                n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _notificationRepo.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _notificationRepo.MarkAllAsReadAsync(userId);
            
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var unreadAnnouncements = await _notificationRepo.GetActiveAnnouncementsAsync(thirtyDaysAgo, thirtyDaysAgo);
            
            if (unreadAnnouncements.Any())
            {
                var readIds = await _notificationRepo.GetReadAnnouncementIdsAsync(userId);
                var unreadIds = unreadAnnouncements
                    .Select(a => a.AnnouncementId)
                    .Except(readIds);
                    
                if (unreadIds.Any())
                {
                    await _notificationRepo.MarkAllAnnouncementsAsReadAsync(userId, unreadIds);
                }
            }
        }

        public async Task CreateAndPushAsync(
            Guid recipientUserId,
            string title,
            string message,
            string type,
            Guid? relatedId = null)
        {
            // 1. Persist first — the notification must survive even if the user is offline
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = recipientUserId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedId = relatedId
            };

            await _notificationRepo.AddAsync(notification);
            await _notificationRepo.SaveChangesAsync();

            // 2. Push via SignalR — fire-and-forget if user is offline (no connection = no-op)
            var dto = MapToDto(notification);
            await _notificationPusher.PushToUserAsync(recipientUserId.ToString(), dto);
        }

        private static NotificationDto MapToDto(Notification n) => new NotificationDto
        {
            NotificationId = n.NotificationId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            RelatedId = n.RelatedId
        };
    }
}
