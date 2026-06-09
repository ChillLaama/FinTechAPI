using System.ComponentModel.DataAnnotations;

namespace FinTechAPI.Application.DTOs
{
    public class FraudEvaluationDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? PaymentId { get; set; }
        public string? TransactionId { get; set; }
        public int FraudScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public List<string> Reasons { get; set; } = new();
        public List<string> RulesTriggered { get; set; } = new();
        public string RulesVersion { get; set; } = string.Empty;
        public long AmountMinorUnits { get; set; }
        public string Currency { get; set; } = string.Empty;
        public double? MlAnomalyScore { get; set; }
        public string? MlModelVersion { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FraudCheckResultDto
    {
        public string EvaluationId { get; set; } = string.Empty;
        public int FraudScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public List<string> Reasons { get; set; } = new();
        public List<string> RulesTriggered { get; set; } = new();
        public double? MlAnomalyScore { get; set; }
        public string? MlModelVersion { get; set; }
    }
}

