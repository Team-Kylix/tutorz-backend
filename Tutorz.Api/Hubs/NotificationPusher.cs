using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Hubs
{
    public class NotificationPusher : INotificationPusher
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationPusher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PushToUserAsync(string userId, object notification)
        {
            // Use SignalR to push the notification specifically to this user
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }
    }
}
