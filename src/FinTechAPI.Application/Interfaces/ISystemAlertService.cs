using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface ISystemAlertService
    {
        Task CreateAsync(string type, string title, string message, string severity,
            string? entityType = null, string? entityId = null);

        Task<IReadOnlyList<SystemAlertDto>> GetActiveAlertsAsync(int limit = 50);

        Task DismissAsync(string alertId);
    }
}

