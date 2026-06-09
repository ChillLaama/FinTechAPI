namespace FinTechAPI.Application.DTOs
{
    public class PaymentDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? LastWebhookEvent { get; set; }
        public string? LastStripeEventId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

