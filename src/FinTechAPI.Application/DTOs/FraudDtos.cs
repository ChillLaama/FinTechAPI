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
    }

    public class FraudCaseDto
    {
        public string Id { get; set; } = string.Empty;
        public string EvaluationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? PaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int FraudScore { get; set; }
        public long AmountMinorUnits { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? Assignee { get; set; }
        public List<string> Reasons { get; set; } = new();
        public List<string> RulesTriggered { get; set; } = new();
        public string? AnalystNotes { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateFraudCaseDto
    {
        [StringLength(2000)]
        public string? AnalystNotes { get; set; }
    }

    public class AssignFraudCaseDto
    {
        [Required]
        [StringLength(256)]
        public string Assignee { get; set; } = string.Empty;
    }

    public class FraudCasePageDto
    {
        public List<FraudCaseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
