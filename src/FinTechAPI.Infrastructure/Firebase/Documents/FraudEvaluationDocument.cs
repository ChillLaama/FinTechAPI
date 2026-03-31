using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class FraudEvaluationDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("paymentId")]
        public string? PaymentId { get; set; }

        [FirestoreProperty("transactionId")]
        public string? TransactionId { get; set; }

        [FirestoreProperty("fraudScore")]
        public int FraudScore { get; set; }

        [FirestoreProperty("riskLevel")]
        public string RiskLevel { get; set; } = string.Empty;

        [FirestoreProperty("decision")]
        public string Decision { get; set; } = string.Empty;

        [FirestoreProperty("reasons")]
        public List<string> Reasons { get; set; } = new();

        [FirestoreProperty("rulesTriggered")]
        public List<string> RulesTriggered { get; set; } = new();

        [FirestoreProperty("rulesVersion")]
        public string RulesVersion { get; set; } = "1.0";

        [FirestoreProperty("correlationId")]
        public string? CorrelationId { get; set; }

        [FirestoreProperty("amountMinorUnits")]
        public long AmountMinorUnits { get; set; }

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = string.Empty;

        [FirestoreProperty("mlAnomalyScore")]
        public double? MlAnomalyScore { get; set; }

        [FirestoreProperty("mlModelVersion")]
        public string? MlModelVersion { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }
    }
}
