namespace FinTechAPI.Infrastructure.ML
{
    public class FraudMlSettings
    {
        public string ModelPath { get; set; } = "ML/Models/fraud_fasttree.onnx";
        public bool Enabled { get; set; } = true;
    }
}
