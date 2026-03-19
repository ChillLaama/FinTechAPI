using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IPlatformBalanceService
    {
        Task<PlatformBalanceDto> GetPlatformBalanceAsync(string currency, CancellationToken cancellationToken = default);
    }
}