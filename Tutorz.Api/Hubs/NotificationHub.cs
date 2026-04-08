using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Tutorz.Api.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time notifications.
    ///
    /// Intentionally thin — it only manages WebSocket connections.
    /// All message pushing is done externally via INotificationPusher / IHubContext,
    /// so the service layer never needs a direct Hub reference.
    ///
    /// ASP.NET Core automatically maps each connection to the authenticated user's
    /// UserId (from the JWT "sub" / NameIdentifier claim) via the built-in
    /// IUserIdProvider. Clients.User(userId) therefore targets ALL open tabs for
    /// that user simultaneously.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
