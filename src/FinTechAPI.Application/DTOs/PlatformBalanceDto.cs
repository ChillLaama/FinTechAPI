namespace FinTechAPI.Application.DTOs
{
    public class PlatformBalanceDto
    {
        public decimal Available { get; set; }
        public decimal Pending { get; set; }
        public string Currency { get; set; } = "usd";
        public string Source { get; set; } = "stripe";
        public DateTime SyncedAt { get; set; }
    }
}