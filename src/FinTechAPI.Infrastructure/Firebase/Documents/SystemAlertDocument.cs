using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class SystemAlertDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("type")]
        public string Type { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>info | warning | critical</summary>
        [FirestoreProperty("severity")]
        public string Severity { get; set; } = "info";

        [FirestoreProperty("isDismissed")]
        public bool IsDismissed { get; set; }

        [FirestoreProperty("entityType")]
        public string? EntityType { get; set; }

        [FirestoreProperty("entityId")]
        public string? EntityId { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }
    }
}

