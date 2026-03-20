using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IPayoutService
    {
        Task<PayoutDto> CreatePayoutAsync(CreatePayoutDto dto, string userId, string idempotencyKey);
        Task<PayoutDto?> GetPayoutByIdAsync(string payoutId, string userId);
        Task<IEnumerable<PayoutDto>> GetPayoutsByUserIdAsync(string userId);
        Task<PayoutDto?> ReconcilePayoutAsync(string payoutId, string userId);
    }
}
