namespace FinTechAPI.Application.DTOs
{
    public class PlatformSummaryDto
    {
        public decimal ProcessedVolume { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public int PendingReviewCount { get; set; }
        public int FraudBlockedCount { get; set; }
        public string Currency { get; set; } = "usd";
        public string Source { get; set; } = "fintechapi+stripe";
        public DateTime SyncedAt { get; set; }
    }
}

