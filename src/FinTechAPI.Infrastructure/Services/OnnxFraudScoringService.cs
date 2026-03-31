using System.Diagnostics;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.ML;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace FinTechAPI.Infrastructure.Services
{
    public sealed class OnnxFraudScoringService : IFraudMlService, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly ILogger<OnnxFraudScoringService> _logger;
        private readonly bool _enabled;
        private readonly string _modelVersion;

        public bool IsModelLoaded => _session != null && _enabled;

        public OnnxFraudScoringService(
            IOptions<FraudMlSettings> options,
            ILogger<OnnxFraudScoringService> logger)
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
                    "ONNX fraud model not found at {ModelPath}. ML scoring will be skipped",
                    modelPath);
                _modelVersion = "not-found";
                return;
            }

            try
            {
                _session = new InferenceSession(modelPath);
                _modelVersion = $"fasttree-onnx-{new FileInfo(modelPath).LastWriteTimeUtc:yyyyMMdd}";
                _logger.LogInformation(
                    "ONNX fraud model loaded successfully. Path={ModelPath}, Version={ModelVersion}",
                    modelPath, _modelVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load ONNX fraud model from {ModelPath}", modelPath);
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

            var inputData = new float[]
            {
                features.Amount,
                features.TypeEncoded,
                features.OldBalanceOrg,
                features.NewBalanceOrig,
                features.OldBalanceDest,
                features.NewBalanceDest,
                features.BalanceDeltaOrg,
                features.BalanceDeltaDest,
                features.AmountToBalanceRatio,
                features.HourOfDay
            };

            var inputTensor = new DenseTensor<float>(inputData, new[] { 1, inputData.Length });

            var inputName = _session!.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
            };

            try
            {
                using var results = _session.Run(inputs);

                // FastTree ONNX output: "Score" (raw score) and "Probability" columns
                float probability = 0f;
                bool predictedLabel = false;

                foreach (var result in results)
                {
                    if (result.Name == "Probability" || result.Name == "probability")
                    {
                        var probTensor = result.AsTensor<float>();
                        probability = probTensor.First();
                    }
                    else if (result.Name == "PredictedLabel" || result.Name == "predicted_label")
                    {
                        var predTensor = result.AsTensor<bool>();
                        predictedLabel = predTensor.First();
                    }
                    else if (result.Name == "Score" || result.Name == "score")
                    {
                        // Fallback: use sigmoid of raw score if no probability output
                        if (probability == 0f)
                        {
                            var scoreTensor = result.AsTensor<float>();
                            var rawScore = scoreTensor.First();
                            probability = 1f / (1f + MathF.Exp(-rawScore));
                        }
                    }
                }

                sw.Stop();

                // Clamp to [0, 1]
                probability = Math.Clamp(probability, 0f, 1f);

                _logger.LogDebug(
                    "ML inference completed. Score={Score:F4}, IsAnomaly={IsAnomaly}, TimeMs={TimeMs}",
                    probability, predictedLabel, sw.ElapsedMilliseconds);

                return Task.FromResult(new FraudMlScoreDto
                {
                    AnomalyScore = probability,
                    IsAnomaly = predictedLabel || probability >= 0.5f,
                    ModelVersion = _modelVersion,
                    InferenceTimeMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "ONNX inference failed");

                return Task.FromResult(new FraudMlScoreDto
                {
                    AnomalyScore = 0f,
                    IsAnomaly = false,
                    ModelVersion = _modelVersion,
                    InferenceTimeMs = sw.ElapsedMilliseconds
                });
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
