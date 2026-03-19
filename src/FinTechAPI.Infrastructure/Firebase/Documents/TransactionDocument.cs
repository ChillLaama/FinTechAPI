using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class TransactionDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("amount")]
        public double Amount { get; set; }

        [FirestoreProperty("currency")]
        public int Currency { get; set; }

        [FirestoreProperty("type")]
        public int Type { get; set; }

        [FirestoreProperty("status")]
        public int Status { get; set; } = 1;

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string? Description { get; set; }

        [FirestoreProperty("transactionDate")]
        public Timestamp TransactionDate { get; set; }

        [FirestoreProperty("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
