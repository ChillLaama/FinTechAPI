using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IFraudService
    {
        Task<FraudCheckResultDto> EvaluateAsync(string userId, long amountMinorUnits, string currency,
            string? paymentId = null, string? transactionId = null, string? correlationId = null);

        Task<FraudEvaluationDto?> GetEvaluationByIdAsync(string evaluationId);
        Task<IEnumerable<FraudEvaluationDto>> GetEvaluationsByUserIdAsync(string userId);
    }
}
