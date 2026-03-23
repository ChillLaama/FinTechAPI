using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class AuditLogDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("action")]
        public string Action { get; set; } = string.Empty;

        [FirestoreProperty("entityType")]
        public string EntityType { get; set; } = string.Empty;

        [FirestoreProperty("entityId")]
        public string? EntityId { get; set; }

        [FirestoreProperty("details")]
        public string? Details { get; set; }

        [FirestoreProperty("correlationId")]
        public string? CorrelationId { get; set; }

        [FirestoreProperty("timestamp")]
        public Timestamp Timestamp { get; set; }
    }
}
