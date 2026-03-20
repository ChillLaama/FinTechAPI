using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IPlatformSummaryService
    {
        Task<PlatformSummaryDto> GetPlatformSummaryAsync(string userId, string currency, CancellationToken cancellationToken = default);
    }
}