using System.Text.Json;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace FinTechAPI.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly FirestoreProvider _firestore;
        private readonly ILogger<AuditService> _logger;

        public AuditService(FirestoreProvider firestore, ILogger<AuditService> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public async Task LogAsync(string userId, string action, string entityType, string? entityId = null,
            object? details = null, string? correlationId = null)
        {
            try
            {
                var doc = new AuditLogDocument
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details is not null ? JsonSerializer.Serialize(details) : null,
                    CorrelationId = correlationId,
                    Timestamp = Timestamp.GetCurrentTimestamp()
                };

                await _firestore.AuditLogs.AddAsync(doc);
            }
            catch (Exception ex)
            {
                // Audit logging should never crash the main operation
                _logger.LogError(ex, "Failed to write audit log: {Action} {EntityType}/{EntityId} for user {UserId}",
                    action, entityType, entityId, userId);
            }
        }
    }
}
