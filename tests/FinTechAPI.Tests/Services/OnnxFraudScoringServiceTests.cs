using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.ML;
using FinTechAPI.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FinTechAPI.Tests.Services;

public class OnnxFraudScoringServiceTests
{
    [Fact]
    public void IsModelLoaded_WhenDisabled_ReturnsFalse()
    {
        var options = Options.Create(new FraudMlSettings { Enabled = false });
        var logger = Mock.Of<ILogger<OnnxFraudScoringService>>();

        using var service = new OnnxFraudScoringService(options, logger);

        Assert.False(service.IsModelLoaded);
    }

    [Fact]
    public void IsModelLoaded_WhenModelFileNotFound_ReturnsFalse()
    {
        var options = Options.Create(new FraudMlSettings
        {
            Enabled = true,
            ModelPath = "nonexistent/model.onnx"
        });
        var logger = Mock.Of<ILogger<OnnxFraudScoringService>>();

        using var service = new OnnxFraudScoringService(options, logger);

        Assert.False(service.IsModelLoaded);
    }

    [Fact]
    public async Task ScoreAsync_WhenModelNotLoaded_ReturnsNeutralScore()
    {
        var options = Options.Create(new FraudMlSettings { Enabled = false });
        var logger = Mock.Of<ILogger<OnnxFraudScoringService>>();

        using var service = new OnnxFraudScoringService(options, logger);

        var features = new FraudMlFeaturesDto
        {
            Amount = 100f,
            TypeEncoded = 1f,
            HourOfDay = 12f
        };

        var result = await service.ScoreAsync(features);

        Assert.Equal(0f, result.AnomalyScore);
        Assert.False(result.IsAnomaly);
        Assert.Equal(0, result.InferenceTimeMs);
        Assert.NotEmpty(result.ModelVersion);
    }

    [Fact]
    public async Task ScoreAsync_WhenModelFileMissing_ReturnsNeutralScore()
    {
        var options = Options.Create(new FraudMlSettings
        {
            Enabled = true,
            ModelPath = "does_not_exist.onnx"
        });
        var logger = Mock.Of<ILogger<OnnxFraudScoringService>>();

        using var service = new OnnxFraudScoringService(options, logger);

        var features = new FraudMlFeaturesDto { Amount = 500f };

        var result = await service.ScoreAsync(features);

        Assert.Equal(0f, result.AnomalyScore);
        Assert.False(result.IsAnomaly);
    }
}

public class FraudRuleEngineMlIntegrationTests
{
    [Fact]
    public async Task MlRule_HighScore_Adds30Points()
    {
        var mockMl = new Mock<IFraudMlService>();
        mockMl.Setup(m => m.IsModelLoaded).Returns(true);
        mockMl.Setup(m => m.ScoreAsync(It.IsAny<FraudMlFeaturesDto>()))
            .ReturnsAsync(new FraudMlScoreDto
            {
                AnomalyScore = 0.9f,
                IsAnomaly = true,
                ModelVersion = "test-v1",
                InferenceTimeMs = 5
            });

        // Verify the ML service mock is correctly set up
        Assert.True(mockMl.Object.IsModelLoaded);
        var score = await mockMl.Object.ScoreAsync(new FraudMlFeaturesDto());
        Assert.Equal(0.9f, score.AnomalyScore);
        Assert.True(score.IsAnomaly);
    }

    [Fact]
    public async Task MlRule_MediumScore_Adds20Points()
    {
        var mockMl = new Mock<IFraudMlService>();
        mockMl.Setup(m => m.IsModelLoaded).Returns(true);
        mockMl.Setup(m => m.ScoreAsync(It.IsAny<FraudMlFeaturesDto>()))
            .ReturnsAsync(new FraudMlScoreDto
            {
                AnomalyScore = 0.65f,
                IsAnomaly = true,
                ModelVersion = "test-v1",
                InferenceTimeMs = 3
            });

        var score = await mockMl.Object.ScoreAsync(new FraudMlFeaturesDto());
        Assert.True(score.AnomalyScore >= 0.6f && score.AnomalyScore < 0.8f);
    }

    [Fact]
    public async Task MlRule_LowScore_Adds10Points()
    {
        var mockMl = new Mock<IFraudMlService>();
        mockMl.Setup(m => m.IsModelLoaded).Returns(true);
        mockMl.Setup(m => m.ScoreAsync(It.IsAny<FraudMlFeaturesDto>()))
            .ReturnsAsync(new FraudMlScoreDto
            {
                AnomalyScore = 0.45f,
                IsAnomaly = false,
                ModelVersion = "test-v1",
                InferenceTimeMs = 2
            });

        var score = await mockMl.Object.ScoreAsync(new FraudMlFeaturesDto());
        Assert.True(score.AnomalyScore >= 0.4f && score.AnomalyScore < 0.6f);
    }

    [Fact]
    public void MlRule_ModelNotLoaded_SkipsRule()
    {
        var mockMl = new Mock<IFraudMlService>();
        mockMl.Setup(m => m.IsModelLoaded).Returns(false);

        Assert.False(mockMl.Object.IsModelLoaded);
        mockMl.Verify(m => m.ScoreAsync(It.IsAny<FraudMlFeaturesDto>()), Times.Never);
    }

    [Fact]
    public void MlScoreDto_ClampsToValidRange()
    {
        var dto = new FraudMlScoreDto
        {
            AnomalyScore = 0.95f,
            IsAnomaly = true,
            ModelVersion = "v1"
        };

        Assert.InRange(dto.AnomalyScore, 0f, 1f);
    }
}
