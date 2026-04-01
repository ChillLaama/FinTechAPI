using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IFraudCaseService
    {
        Task<FraudCaseDto> CreateCaseAsync(string evaluationId, string userId, string? paymentId,
            string riskLevel, int fraudScore, long amountMinorUnits, string currency,
            List<string> reasons, List<string> rulesTriggered,
            double? mlAnomalyScore = null, string? mlModelVersion = null,
            string? correlationId = null);

        Task<FraudCasePageDto> GetCasesAsync(string? status = null, int limit = 20, string? startAfter = null);
        Task<FraudCaseDto?> GetCaseByIdAsync(string caseId);
        Task<FraudCaseDto?> ApproveCaseAsync(string caseId, string resolvedBy, string? notes = null, string? correlationId = null);
        Task<FraudCaseDto?> RejectCaseAsync(string caseId, string resolvedBy, string? notes = null, string? correlationId = null);
        Task<FraudCaseDto?> EscalateCaseAsync(string caseId, string? notes = null, string? correlationId = null);
        Task<FraudCaseDto?> AssignCaseAsync(string caseId, string assignee, string? correlationId = null);
    }
}
