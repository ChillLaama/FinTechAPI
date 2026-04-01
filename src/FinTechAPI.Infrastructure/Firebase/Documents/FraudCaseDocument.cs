using Google.Cloud.Firestore;

namespace FinTechAPI.Infrastructure.Firebase.Documents
{
    [FirestoreData]
    public class FraudCaseDocument
    {
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("evaluationId")]
        public string EvaluationId { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("paymentId")]
        public string? PaymentId { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = "open";

        [FirestoreProperty("riskLevel")]
        public string RiskLevel { get; set; } = string.Empty;

        [FirestoreProperty("fraudScore")]
        public int FraudScore { get; set; }

        [FirestoreProperty("amountMinorUnits")]
        public long AmountMinorUnits { get; set; }

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = string.Empty;

        [FirestoreProperty("assignee")]
        public string? Assignee { get; set; }

        [FirestoreProperty("reasons")]
        public List<string> Reasons { get; set; } = new();

        [FirestoreProperty("rulesTriggered")]
        public List<string> RulesTriggered { get; set; } = new();

        [FirestoreProperty("mlAnomalyScore")]
        public double? MlAnomalyScore { get; set; }

        [FirestoreProperty("mlModelVersion")]
        public string? MlModelVersion { get; set; }

        [FirestoreProperty("analystNotes")]
        public string? AnalystNotes { get; set; }

        [FirestoreProperty("resolvedBy")]
        public string? ResolvedBy { get; set; }

        [FirestoreProperty("resolvedAt")]
        public Timestamp? ResolvedAt { get; set; }

        [FirestoreProperty("correlationId")]
        public string? CorrelationId { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; }
    }
}
