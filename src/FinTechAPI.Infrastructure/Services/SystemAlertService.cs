using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace FinTechAPI.Infrastructure.Services
{
    public class SystemAlertService : ISystemAlertService
    {
        private readonly FirestoreProvider _firestore;
        private readonly ILogger<SystemAlertService> _logger;

        public SystemAlertService(FirestoreProvider firestore, ILogger<SystemAlertService> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public async Task CreateAsync(string type, string title, string message, string severity,
            string? entityType = null, string? entityId = null)
        {
            try
            {
                var docRef = _firestore.SystemAlerts.Document();
                var doc = new SystemAlertDocument
                {
                    Id = docRef.Id,
                    Type = type,
                    Title = title,
                    Message = message,
                    Severity = severity,
                    IsDismissed = false,
                    EntityType = entityType,
                    EntityId = entityId,
                    CreatedAt = Timestamp.GetCurrentTimestamp()
                };

                await docRef.SetAsync(doc);

                _logger.LogInformation(
                    "System alert created. AlertId={AlertId}, Type={Type}, Severity={Severity}, Title={Title}",
                    doc.Id, type, severity, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create system alert. Type={Type}, Title={Title}", type, title);
            }
        }

        public async Task<IReadOnlyList<SystemAlertDto>> GetActiveAlertsAsync(int limit = 50)
        {
            var snapshot = await _firestore.SystemAlerts
                .WhereEqualTo("isDismissed", false)
                .OrderByDescending("createdAt")
                .Limit(limit)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<SystemAlertDocument>())
                .Select(a => new SystemAlertDto
                {
                    Id = a.Id,
                    Type = a.Type,
                    Title = a.Title,
                    Message = a.Message,
                    Severity = a.Severity,
                    IsDismissed = a.IsDismissed,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    CreatedAt = a.CreatedAt.ToDateTime()
                })
                .ToList();
        }

        public async Task DismissAsync(string alertId)
        {
            var docRef = _firestore.SystemAlerts.Document(alertId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return;

            await docRef.UpdateAsync("isDismissed", true);

            _logger.LogInformation("System alert dismissed. AlertId={AlertId}", alertId);
        }
    }
}

