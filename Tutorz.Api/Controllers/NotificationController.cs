using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }

        /// <summary>
        /// GET /api/notification
        /// Returns the latest 50 notifications for the logged-in user, newest first.
        /// Called on app load to populate notification history.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notifications = await _notificationService.GetForUserAsync(userId.Value);
            return Ok(notifications);
        }

        /// <summary>
        /// PUT /api/notification/{id}/read
        /// Marks a single notification as read.
        /// </summary>
        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId.Value);
            return Ok(new { message = "Marked as read." });
        }

        /// <summary>
        /// PUT /api/notification/read-all
        /// Marks ALL notifications for the logged-in user as read (single DB update).
        /// </summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId.Value);
            return Ok(new { message = "All notifications marked as read." });
        }
    }
}
