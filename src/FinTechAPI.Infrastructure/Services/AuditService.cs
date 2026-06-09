using System.Text.Json;
using FinTechAPI.Application.DTOs;
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

        public async Task<IReadOnlyList<AuditLogDto>> QueryAsync(AuditLogQueryDto query)
        {
            Query q = _firestore.AuditLogs.OrderByDescending("timestamp");

            if (!string.IsNullOrWhiteSpace(query.UserId))
                q = q.WhereEqualTo("userId", query.UserId);

            if (!string.IsNullOrWhiteSpace(query.EntityType))
                q = q.WhereEqualTo("entityType", query.EntityType);

            if (!string.IsNullOrWhiteSpace(query.Action))
                q = q.WhereEqualTo("action", query.Action);

            if (query.From.HasValue)
                q = q.WhereGreaterThanOrEqualTo("timestamp", Timestamp.FromDateTime(query.From.Value.ToUniversalTime()));

            if (query.To.HasValue)
                q = q.WhereLessThanOrEqualTo("timestamp", Timestamp.FromDateTime(query.To.Value.ToUniversalTime()));

            var limit = Math.Clamp(query.Limit, 1, 200);
            var snapshot = await q.Limit(limit).GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<AuditLogDocument>())
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Details = a.Details,
                    CorrelationId = a.CorrelationId,
                    Timestamp = a.Timestamp.ToDateTime()
                })
                .ToList();
        }
    }
}
