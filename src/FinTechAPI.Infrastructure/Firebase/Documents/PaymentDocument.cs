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

        /// <summary>Amount in minor currency units (e.g. cents). Stored as integer to avoid floating-point precision loss.</summary>
        [FirestoreProperty("amountMinorUnits")]
        public long AmountMinorUnits { get; set; }

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

        /// <summary>Stripe Event ID of the most-recently processed webhook, used for deduplication.</summary>
        [FirestoreProperty("lastStripeEventId")]
        public string? LastStripeEventId { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
