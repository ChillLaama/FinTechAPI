using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class PayoutDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("amountMinorUnits")]
        public long AmountMinorUnits { get; set; }

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("stripePayoutId")]
        public string StripePayoutId { get; set; } = string.Empty;

        [FirestoreProperty("stripeAccountId")]
        public string? StripeAccountId { get; set; }

        [FirestoreProperty("reserveId")]
        public string ReserveId { get; set; } = string.Empty;

        [FirestoreProperty("reserveStatus")]
        public string ReserveStatus { get; set; } = string.Empty;

        [FirestoreProperty("failureCode")]
        public string? FailureCode { get; set; }

        [FirestoreProperty("failureMessage")]
        public string? FailureMessage { get; set; }

        [FirestoreProperty("externalReference")]
        public string? ExternalReference { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
