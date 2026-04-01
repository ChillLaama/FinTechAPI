using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace FinTechAPI.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly FirestoreProvider _firestore;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(FirestoreProvider firestore, ILogger<NotificationService> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public async Task SendAsync(string userId, string type, string title, string message,
            string? entityType = null, string? entityId = null)
        {
            try
            {
                var doc = new NotificationDocument
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    EntityType = entityType,
                    EntityId = entityId,
                    IsRead = false,
                    CreatedAt = Timestamp.GetCurrentTimestamp()
                };

                await _firestore.Notifications.AddAsync(doc);

                _logger.LogInformation(
                    "Notification sent. UserId={UserId}, Type={Type}, Title={Title}",
                    userId, type, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send notification. UserId={UserId}, Type={Type}",
                    userId, type);
            }
        }

        public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(string userId, int limit = 50)
        {
            var snapshot = await _firestore.Notifications
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<NotificationDocument>())
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        EntityType = n.EntityType,
                        EntityId = n.EntityId,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt.ToDateTime()
                    })
                .ToList();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var snapshot = await _firestore.Notifications
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("isRead", false)
                .GetSnapshotAsync();

            return snapshot.Count;
        }

        public async Task MarkAsReadAsync(string notificationId, string userId)
        {
            var docRef = _firestore.Notifications.Document(notificationId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return;

            var doc = snapshot.ConvertTo<NotificationDocument>();
            if (!string.Equals(doc.UserId, userId, StringComparison.Ordinal))
                return;

            await docRef.UpdateAsync("isRead", true);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var snapshot = await _firestore.Notifications
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("isRead", false)
                .GetSnapshotAsync();

            var batch = _firestore.Db.StartBatch();
            foreach (var doc in snapshot.Documents)
            {
                batch.Update(doc.Reference, "isRead", true);
            }

            await batch.CommitAsync();

            _logger.LogInformation(
                "Marked {Count} notifications as read for user {UserId}",
                snapshot.Count, userId);
        }
    }
}
