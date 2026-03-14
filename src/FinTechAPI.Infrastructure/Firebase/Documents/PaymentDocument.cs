using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class PaymentDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("amount")]
        public double Amount { get; set; }

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("stripePaymentIntentId")]
        public string StripePaymentIntentId { get; set; } = string.Empty;

        [FirestoreProperty("transactionId")]
        public string? TransactionId { get; set; }

        [FirestoreProperty("lastWebhookEvent")]
        public string? LastWebhookEvent { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
