using Microsoft.ML.Data;

namespace FinTechAPI.MlTrainer;

public class FraudTransactionData
{
    [LoadColumn(0)]
    public float Step { get; set; }

    [LoadColumn(1)]
    public string Type { get; set; } = string.Empty;

    [LoadColumn(2)]
    public float Amount { get; set; }

    [LoadColumn(3)]
    public string NameOrig { get; set; } = string.Empty;

    [LoadColumn(4)]
    public float OldBalanceOrg { get; set; }

    [LoadColumn(5)]
    public float NewBalanceOrig { get; set; }

    [LoadColumn(6)]
    public string NameDest { get; set; } = string.Empty;

    [LoadColumn(7)]
    public float OldBalanceDest { get; set; }

    [LoadColumn(8)]
    public float NewBalanceDest { get; set; }

    [LoadColumn(9)]
    [ColumnName("Label")]
    public bool IsFraud { get; set; }

    [LoadColumn(10)]
    public float IsFlaggedFraud { get; set; }
}

public class FraudPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Score { get; set; }

    public float Probability { get; set; }
}
