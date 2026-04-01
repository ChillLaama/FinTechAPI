using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class NotificationDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("type")]
        public string Type { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("message")]
        public string Message { get; set; } = string.Empty;

        [FirestoreProperty("entityType")]
        public string? EntityType { get; set; }

        [FirestoreProperty("entityId")]
        public string? EntityId { get; set; }

        [FirestoreProperty("isRead")]
        public bool IsRead { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }
    }
}
