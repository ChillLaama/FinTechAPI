using System.Diagnostics;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using FinTechAPI.MlTrainer;

// Resolve repo root: walk up from the executable directory until we find FinTechAPI.sln
static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir, "FinTechAPI.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    // Fallback: try working directory
    dir = Directory.GetCurrentDirectory();
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir, "FinTechAPI.sln")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    return Directory.GetCurrentDirectory();
}

var repoRoot = FindRepoRoot();
var csvPath = Path.Combine(repoRoot, "data", "Fraud.csv");
const string ModelsDir = "models";

if (!File.Exists(csvPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: Dataset not found at '{csvPath}'.");
    Console.WriteLine("Download Fraud.csv from https://www.kaggle.com/datasets/chitwanmanchanda/fraudulent-transactions-data");
    Console.WriteLine("and place it in the data/ folder at the repository root.");
    Console.ResetColor();
    return 1;
}

Directory.CreateDirectory(ModelsDir);

var mlContext = new MLContext(seed: 42);

// ── 1. Load data ─────────────────────────────────────────────────────────
Console.WriteLine("Loading dataset...");
var sw = Stopwatch.StartNew();
var dataView = mlContext.Data.LoadFromTextFile<FraudTransactionData>(
    csvPath, hasHeader: true, separatorChar: ',');

var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, seed: 42);
sw.Stop();
Console.WriteLine($"Dataset loaded in {sw.Elapsed.TotalSeconds:F1}s");
Console.WriteLine($"  Train rows: {split.TrainSet.GetRowCount()}");
Console.WriteLine($"  Test  rows: {split.TestSet.GetRowCount()}");

// ── 2. Feature engineering pipeline ──────────────────────────────────────
Console.WriteLine("\nBuilding feature pipeline...");

var featurePipeline = mlContext.Transforms.Categorical
    .OneHotEncoding("TypeEncoded", "Type")
    .Append(mlContext.Transforms.Concatenate("Features",
        "Amount", "TypeEncoded", "OldBalanceOrg", "NewBalanceOrig",
        "OldBalanceDest", "NewBalanceDest", "Step"))
    .Append(mlContext.Transforms.NormalizeMinMax("Features"));

// ── 3. Model A: FastTree (supervised binary classification) ──────────────
Console.WriteLine("\n══════════════════════════════════════════════════");
Console.WriteLine("  Model A: FastTree Binary Classification");
Console.WriteLine("══════════════════════════════════════════════════");

sw.Restart();
var fastTreePipeline = featurePipeline.Append(
    mlContext.BinaryClassification.Trainers.FastTree(
        new FastTreeBinaryTrainer.Options
        {
            LabelColumnName = "Label",
            FeatureColumnName = "Features",
            NumberOfLeaves = 20,
            NumberOfTrees = 100,
            MinimumExampleCountPerLeaf = 10,
            LearningRate = 0.1
        }));

Console.WriteLine("Training FastTree model...");
var fastTreeModel = fastTreePipeline.Fit(split.TrainSet);
sw.Stop();
Console.WriteLine($"Training completed in {sw.Elapsed.TotalSeconds:F1}s");

Console.WriteLine("Evaluating FastTree...");
var fastTreePredictions = fastTreeModel.Transform(split.TestSet);
var fastTreeMetrics = mlContext.BinaryClassification.Evaluate(fastTreePredictions, labelColumnName: "Label");

PrintBinaryMetrics(fastTreeMetrics);

// Save .zip and ONNX
var fastTreeZipPath = Path.Combine(ModelsDir, "fraud_fasttree.zip");
mlContext.Model.Save(fastTreeModel, split.TrainSet.Schema, fastTreeZipPath);
Console.WriteLine($"  Saved: {fastTreeZipPath}");

var fastTreeOnnxPath = Path.Combine(ModelsDir, "fraud_fasttree.onnx");
try
{
    using var onnxStream = File.Create(fastTreeOnnxPath);
    mlContext.Model.ConvertToOnnx(fastTreeModel, split.TrainSet, onnxStream);
    Console.WriteLine($"  Saved: {fastTreeOnnxPath} ({new FileInfo(fastTreeOnnxPath).Length / 1024} KB)");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"  ONNX export skipped (ML.NET converter limitation): {ex.Message}");
    Console.WriteLine($"  Using .zip model for inference instead.");
    Console.ResetColor();
    if (File.Exists(fastTreeOnnxPath)) File.Delete(fastTreeOnnxPath);
}

// ── 4. Model B: RandomizedPCA (unsupervised anomaly detection) ───────────
Console.WriteLine("\n══════════════════════════════════════════════════");
Console.WriteLine("  Model B: Randomized PCA Anomaly Detection");
Console.WriteLine("══════════════════════════════════════════════════");

sw.Restart();
var pcaPipeline = featurePipeline.Append(
    mlContext.AnomalyDetection.Trainers.RandomizedPca(
        featureColumnName: "Features",
        rank: 5,
        oversampling: 20));

Console.WriteLine("Training PCA model...");
var pcaModel = pcaPipeline.Fit(split.TrainSet);
sw.Stop();
Console.WriteLine($"Training completed in {sw.Elapsed.TotalSeconds:F1}s");

Console.WriteLine("Evaluating PCA...");
var pcaPredictions = pcaModel.Transform(split.TestSet);
// Anomaly evaluator requires Label as Single (float), not Boolean
var labelAsFloat = mlContext.Transforms.Conversion
    .ConvertType("Label", outputKind: DataKind.Single)
    .Fit(pcaPredictions).Transform(pcaPredictions);
var pcaMetrics = mlContext.AnomalyDetection.Evaluate(labelAsFloat, labelColumnName: "Label");

Console.WriteLine($"  AUC:                       {pcaMetrics.AreaUnderRocCurve:F4}");
Console.WriteLine($"  Detection rate at FP=10:   {pcaMetrics.DetectionRateAtFalsePositiveCount:F4}");

var pcaZipPath = Path.Combine(ModelsDir, "fraud_pca.zip");
mlContext.Model.Save(pcaModel, split.TrainSet.Schema, pcaZipPath);
Console.WriteLine($"  Saved: {pcaZipPath}");

var pcaOnnxPath = Path.Combine(ModelsDir, "fraud_pca.onnx");
try
{
    using var onnxStream = File.Create(pcaOnnxPath);
    mlContext.Model.ConvertToOnnx(pcaModel, split.TrainSet, onnxStream);
    Console.WriteLine($"  Saved: {pcaOnnxPath} ({new FileInfo(pcaOnnxPath).Length / 1024} KB)");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"  ONNX export skipped: {ex.Message}");
    Console.WriteLine($"  Using .zip model for inference instead.");
    Console.ResetColor();
    if (File.Exists(pcaOnnxPath)) File.Delete(pcaOnnxPath);
}

// ── 5. Write evaluation report ───────────────────────────────────────────
var reportPath = Path.Combine(ModelsDir, "evaluation_report.txt");
using (var writer = new StreamWriter(reportPath))
{
    writer.WriteLine("FinTechAPI Fraud Detection — Model Evaluation Report");
    writer.WriteLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    writer.WriteLine($"Dataset:   {csvPath}");
    writer.WriteLine(new string('═', 60));

    writer.WriteLine("\n## Model A: FastTree Binary Classification");
    writer.WriteLine($"  Accuracy:    {fastTreeMetrics.Accuracy:F4}");
    writer.WriteLine($"  AUC-ROC:     {fastTreeMetrics.AreaUnderRocCurve:F4}");
    writer.WriteLine($"  AUC-PR:      {fastTreeMetrics.AreaUnderPrecisionRecallCurve:F4}");
    writer.WriteLine($"  F1 Score:    {fastTreeMetrics.F1Score:F4}");
    writer.WriteLine($"  Precision:   {fastTreeMetrics.PositivePrecision:F4}");
    writer.WriteLine($"  Recall:      {fastTreeMetrics.PositiveRecall:F4}");
    writer.WriteLine($"  Output:      {fastTreeOnnxPath}");

    writer.WriteLine("\n## Model B: Randomized PCA Anomaly Detection");
    writer.WriteLine($"  AUC-ROC:             {pcaMetrics.AreaUnderRocCurve:F4}");
    writer.WriteLine($"  Detection@FP=10:     {pcaMetrics.DetectionRateAtFalsePositiveCount:F4}");
    writer.WriteLine($"  Output:              {pcaOnnxPath}");

    writer.WriteLine("\n## Recommendation");
    writer.WriteLine("  Use FastTree for production (supervised, higher accuracy on labeled data).");
    writer.WriteLine("  PCA included for thesis comparison of supervised vs unsupervised approaches.");
}
Console.WriteLine($"\nReport saved: {reportPath}");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n✓ Training complete. Copy the .zip file to:");
Console.WriteLine("  src/FinTechAPI.Infrastructure/ML/Models/fraud_fasttree.zip");
Console.ResetColor();

return 0;

// ── Helpers ──────────────────────────────────────────────────────────────

static void PrintBinaryMetrics(BinaryClassificationMetrics metrics)
{
    Console.WriteLine($"  Accuracy:    {metrics.Accuracy:F4}");
    Console.WriteLine($"  AUC-ROC:     {metrics.AreaUnderRocCurve:F4}");
    Console.WriteLine($"  AUC-PR:      {metrics.AreaUnderPrecisionRecallCurve:F4}");
    Console.WriteLine($"  F1 Score:    {metrics.F1Score:F4}");
    Console.WriteLine($"  Precision:   {metrics.PositivePrecision:F4}");
    Console.WriteLine($"  Recall:      {metrics.PositiveRecall:F4}");
}
