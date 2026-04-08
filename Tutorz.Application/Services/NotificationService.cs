using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var notifications = await _notificationRepo.GetLatestForUserAsync(userId);
            return notifications.Select(MapToDto);
        }

        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            // Fetch and verify ownership before marking
            var notification = await _notificationRepo.GetAsync(
                n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null) return; // Silently ignore — wrong owner or not found

            notification.IsRead = true;
            await _notificationRepo.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            // Uses ExecuteUpdateAsync in repo — single SQL UPDATE, no N+1
            await _notificationRepo.MarkAllAsReadAsync(userId);
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
