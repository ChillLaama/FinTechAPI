namespace FinTechAPI.Application.DTOs
{
    public class PaymentIntentResponseDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
    }
}
