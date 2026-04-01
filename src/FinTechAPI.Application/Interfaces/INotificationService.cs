namespace FinTechAPI.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(string userId, string type, string title, string message,
            string? entityType = null, string? entityId = null);

        Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(string userId, int limit = 50);

        Task<int> GetUnreadCountAsync(string userId);

        Task MarkAsReadAsync(string notificationId, string userId);

        Task MarkAllAsReadAsync(string userId);
    }

    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
