namespace FinTechAPI.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string entityType, string? entityId = null,
            object? details = null, string? correlationId = null);
    }
}
