namespace FinTechAPI.Application.DTOs
{
    public class AdminOverviewDto
    {
        public int ActiveAlertsCount { get; set; }
        public int CriticalAlertsCount { get; set; }
        public int PendingPaymentsCount { get; set; }
        public int StuckPaymentsCount { get; set; }
        public int OpenFraudCasesCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}

