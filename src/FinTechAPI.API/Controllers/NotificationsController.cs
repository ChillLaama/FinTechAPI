using System.Security.Claims;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(
            [FromQuery] int limit = 50)
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);
            return Ok(notifications);
        }

        [Authorize]
        [HttpGet("unread-count")]
        public async Task<ActionResult<object>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [Authorize]
        [HttpPatch("{notificationId}/read")]
        public async Task<ActionResult> MarkAsRead(string notificationId)
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(notificationId, userId);
            return NoContent();
        }

        [Authorize]
        [HttpPost("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }
    }
}
