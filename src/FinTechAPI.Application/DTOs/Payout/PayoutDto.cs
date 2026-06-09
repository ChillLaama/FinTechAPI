namespace FinTechAPI.Application.DTOs
{
    public class PayoutDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StripePayoutId { get; set; } = string.Empty;
        public string? StripeAccountId { get; set; }
        public string ReserveStatus { get; set; } = string.Empty;
        public string ReserveId { get; set; } = string.Empty;
        public string? FailureCode { get; set; }
        public string? FailureMessage { get; set; }
        public string? ExternalReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

