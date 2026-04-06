using System;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    /// <summary>
    /// Abstracts the real-time push mechanism (SignalR) away from the Application layer.
    /// The implementation lives in Tutorz.Api (where the Hub is available) and is
    /// registered in Program.cs. This avoids a circular project dependency.
    /// </summary>
    public interface INotificationPusher
    {
        /// <summary>
        /// Pushes a serialized notification to all active connections of the given user.
        /// If the user is offline, this is a silent no-op.
        /// </summary>
        Task PushToUserAsync(string userId, object notification);
    }
}
