namespace FinTechAPI.Application.DTOs
{
    public class FraudMlFeaturesDto
    {
        public float Amount { get; set; }
        public float OldBalanceOrg { get; set; }
        public float NewBalanceOrig { get; set; }
        public float OldBalanceDest { get; set; }
        public float NewBalanceDest { get; set; }
        public float BalanceDeltaOrg { get; set; }
        public float BalanceDeltaDest { get; set; }
        public float AmountToBalanceRatio { get; set; }
        public float HourOfDay { get; set; }
        public float TypeEncoded { get; set; }
    }

    public class FraudMlScoreDto
    {
        public float AnomalyScore { get; set; }
        public bool IsAnomaly { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
        public long InferenceTimeMs { get; set; }
    }
}

