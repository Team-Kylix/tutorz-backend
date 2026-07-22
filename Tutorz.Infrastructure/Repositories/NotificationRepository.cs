using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(TutorzDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetLatestForUserAsync(Guid userId, int take = 50)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            // Batch update — single round-trip, no N+1 issue
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task<IEnumerable<SystemAnnouncement>> GetActiveAnnouncementsAsync(DateTime joinedAt, DateTime cutoff)
        {
            return await _context.SystemAnnouncements
                .Where(a => a.CreatedAt >= cutoff && a.CreatedAt >= joinedAt)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Guid>> GetReadAnnouncementIdsAsync(Guid userId)
        {
            return await _context.UserAnnouncementReads
                .Where(uar => uar.UserId == userId)
                .Select(uar => uar.AnnouncementId)
                .ToListAsync();
        }

        public async Task MarkAnnouncementAsReadAsync(Guid userId, Guid announcementId)
        {
            var isAnnouncement = await _context.SystemAnnouncements.AnyAsync(a => a.AnnouncementId == announcementId);
            if (!isAnnouncement) return;

            var existingRead = await _context.UserAnnouncementReads
                .FirstOrDefaultAsync(uar => uar.UserId == userId && uar.AnnouncementId == announcementId);

            if (existingRead == null)
            {
                _context.UserAnnouncementReads.Add(new UserAnnouncementRead
                {
                    UserId = userId,
                    AnnouncementId = announcementId,
                    ReadAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAnnouncementsAsReadAsync(Guid userId, IEnumerable<Guid> announcementIds)
        {
            var readsToInsert = new List<UserAnnouncementRead>();
            foreach (var id in announcementIds)
            {
                var existing = await _context.UserAnnouncementReads
                    .FirstOrDefaultAsync(uar => uar.UserId == userId && uar.AnnouncementId == id);
                
                if (existing == null)
                {
                    readsToInsert.Add(new UserAnnouncementRead
                    {
                        UserId = userId,
                        AnnouncementId = id,
                        ReadAt = DateTime.UtcNow
                    });
                }
            }

            if (readsToInsert.Any())
            {
                _context.UserAnnouncementReads.AddRange(readsToInsert);
                await _context.SaveChangesAsync();
            }
        }
    }
}
