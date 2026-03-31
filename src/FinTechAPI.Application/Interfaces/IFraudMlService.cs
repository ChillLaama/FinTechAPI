using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IFraudMlService
    {
        Task<FraudMlScoreDto> ScoreAsync(FraudMlFeaturesDto features);
        bool IsModelLoaded { get; }
    }
}
