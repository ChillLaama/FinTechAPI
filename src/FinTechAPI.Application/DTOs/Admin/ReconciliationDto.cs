namespace FinTechAPI.Application.DTOs
{
    public class ReconciliationSummaryDto
    {
        public int PendingPaymentsCount { get; set; }
        public int StuckPaymentsCount { get; set; }
        public int TotalPaymentsCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class PendingPaymentDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public string? LastWebhookEvent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Minutes since last status update.</summary>
        public int StaleMinutes { get; set; }
    }
}

