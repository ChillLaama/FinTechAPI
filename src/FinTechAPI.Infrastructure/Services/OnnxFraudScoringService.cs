using System.Diagnostics;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.ML;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace FinTechAPI.Infrastructure.Services
{
    /// <summary>
    /// Input class matching the trained FastTree model's schema.
    /// </summary>
    public sealed class FraudModelInput
    {
        public float Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public float OldBalanceOrg { get; set; }
        public float NewBalanceOrig { get; set; }
        public float OldBalanceDest { get; set; }
        public float NewBalanceDest { get; set; }
        public float Step { get; set; }
    }

    /// <summary>
    /// Output class for FastTree binary classification predictions.
    /// </summary>
    public sealed class FraudModelOutput
    {
        public bool PredictedLabel { get; set; }
        public float Score { get; set; }
        public float Probability { get; set; }
    }

    public sealed class MlNetFraudScoringService : IFraudMlService
    {
        private readonly PredictionEngine<FraudModelInput, FraudModelOutput>? _engine;
        private readonly ILogger<MlNetFraudScoringService> _logger;
        private readonly bool _enabled;
        private readonly string _modelVersion;

        public bool IsModelLoaded => _engine != null && _enabled;

        public MlNetFraudScoringService(
            IOptions<FraudMlSettings> options,
            ILogger<MlNetFraudScoringService> logger)
        {
            _logger = logger;
            var settings = options.Value;
            _enabled = settings.Enabled;

            if (!_enabled)
            {
                _logger.LogInformation("FraudMl scoring is disabled via configuration");
                _modelVersion = "disabled";
                return;
            }

            var modelPath = Path.IsPathRooted(settings.ModelPath)
                ? settings.ModelPath
                : Path.Combine(AppContext.BaseDirectory, settings.ModelPath);

            if (!File.Exists(modelPath))
            {
                _logger.LogWarning(
                    "Fraud model not found at {ModelPath}. ML scoring will be skipped",
                    modelPath);
                _modelVersion = "not-found";
                return;
            }

            try
            {
                var mlContext = new MLContext();
                var model = mlContext.Model.Load(modelPath, out _);
                _engine = mlContext.Model.CreatePredictionEngine<FraudModelInput, FraudModelOutput>(model);
                _modelVersion = $"fasttree-v{new FileInfo(modelPath).LastWriteTimeUtc:yyyyMMdd}";
                _logger.LogInformation(
                    "Fraud model loaded successfully. Path={ModelPath}, Version={ModelVersion}",
                    modelPath, _modelVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load fraud model from {ModelPath}", modelPath);
                _modelVersion = "load-error";
            }
        }

        public Task<FraudMlScoreDto> ScoreAsync(FraudMlFeaturesDto features)
        {
            if (!IsModelLoaded)
            {
                return Task.FromResult(new FraudMlScoreDto
                {
                    AnomalyScore = 0f,
                    IsAnomaly = false,
                    ModelVersion = _modelVersion,
                    InferenceTimeMs = 0
                });
            }

            var sw = Stopwatch.StartNew();

            try
            {
                var input = new FraudModelInput
                {
                    Amount = features.Amount,
                    Type = features.TypeEncoded switch
                    {
                        1f => "CASH_IN",
                        2f => "CASH_OUT",
                        3f => "DEBIT",
                        4f => "PAYMENT",
                        5f => "TRANSFER",
                        _ => "PAYMENT"
                    },
                    OldBalanceOrg = features.OldBalanceOrg,
                    NewBalanceOrig = features.NewBalanceOrig,
                    OldBalanceDest = features.OldBalanceDest,
                    NewBalanceDest = features.NewBalanceDest,
                    Step = features.HourOfDay
                };

                var prediction = _engine!.Predict(input);
                sw.Stop();

                var probability = Math.Clamp(prediction.Probability, 0f, 1f);

                _logger.LogDebug(
                    "ML inference completed. Score={Score:F4}, IsAnomaly={IsAnomaly}, TimeMs={TimeMs}",
                    probability, prediction.PredictedLabel, sw.ElapsedMilliseconds);

                return Task.FromResult(new FraudMlScoreDto
                {
                    AnomalyScore = probability,
                    IsAnomaly = prediction.PredictedLabel || probability >= 0.5f,
                    ModelVersion = _modelVersion,
                    InferenceTimeMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "ML inference failed");

                return Task.FromResult(new FraudMlScoreDto
                {
                    AnomalyScore = 0f,
                    IsAnomaly = false,
                    ModelVersion = _modelVersion,
                    InferenceTimeMs = sw.ElapsedMilliseconds
                });
            }
        }
    }
}
