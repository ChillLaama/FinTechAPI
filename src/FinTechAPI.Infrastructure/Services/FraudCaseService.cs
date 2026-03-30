using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace FinTechAPI.Infrastructure.Services
{
    public class FraudCaseService : IFraudCaseService
    {
        private readonly FirestoreProvider _firestore;
        private readonly IAuditService _audit;
        private readonly ILogger<FraudCaseService> _logger;

        public FraudCaseService(FirestoreProvider firestore, IAuditService audit, ILogger<FraudCaseService> logger)
        {
            _firestore = firestore;
            _audit = audit;
            _logger = logger;
        }

        public async Task<FraudCaseDto> CreateCaseAsync(
            string evaluationId, string userId, string? paymentId,
            string riskLevel, int fraudScore, long amountMinorUnits, string currency,
            List<string> reasons, List<string> rulesTriggered, string? correlationId = null)
        {
            var now = Timestamp.GetCurrentTimestamp();
            var docRef = _firestore.FraudCases.Document();

            var doc = new FraudCaseDocument
            {
                Id = docRef.Id,
                EvaluationId = evaluationId,
                UserId = userId,
                PaymentId = paymentId,
                Status = FraudCaseStatus.Open.ToString(),
                RiskLevel = riskLevel,
                FraudScore = fraudScore,
                AmountMinorUnits = amountMinorUnits,
                Currency = currency,
                Reasons = reasons,
                RulesTriggered = rulesTriggered,
                CorrelationId = correlationId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await docRef.SetAsync(doc);

            await _audit.LogAsync("system", "fraud_case_created", "FraudCase", doc.Id,
                new { evaluationId, userId, riskLevel, fraudScore }, correlationId);

            _logger.LogInformation(
                "Fraud case created. CaseId={CaseId}, EvaluationId={EvaluationId}, UserId={UserId}, RiskLevel={RiskLevel}, CorrelationId={CorrelationId}",
                doc.Id, evaluationId, userId, riskLevel, correlationId);

            return MapCaseDoc(doc);
        }

        public async Task<FraudCasePageDto> GetCasesAsync(string? status = null, int limit = 20, string? startAfter = null)
        {
            try
            {
                Query query = _firestore.FraudCases.OrderByDescending("createdAt");

                if (!string.IsNullOrEmpty(status))
                    query = query.WhereEqualTo("status", status);

                // Total count (limited to 200 for performance)
                var countSnapshot = await query.Limit(200).GetSnapshotAsync();
                var totalCount = countSnapshot.Count;

                if (!string.IsNullOrEmpty(startAfter))
                {
                    var startDoc = await _firestore.FraudCases.Document(startAfter).GetSnapshotAsync();
                    if (startDoc.Exists)
                        query = query.StartAfter(startDoc);
                }

                var snapshots = await query.Limit(limit).GetSnapshotAsync();
                var items = snapshots.Documents
                    .Select(d => MapCaseDoc(d.ConvertTo<FraudCaseDocument>()))
                    .ToList();

                return new FraudCasePageDto { Items = items, TotalCount = totalCount };
            }
            catch (RpcException ex) when (IsMissingCompositeIndex(ex) && !string.IsNullOrEmpty(status))
            {
                _logger.LogWarning(
                    "Missing Firestore composite index for fraud cases query with status filter. Falling back to in-memory ordering. Status={Status}",
                    status);

                // Fallback path avoids composite index requirement: filter by status only,
                // then sort and paginate in memory (bounded to 200 documents).
                var fallbackSnapshot = await _firestore.FraudCases
                    .WhereEqualTo("status", status)
                    .Limit(200)
                    .GetSnapshotAsync();

                var orderedCases = fallbackSnapshot.Documents
                    .Select(d => d.ConvertTo<FraudCaseDocument>())
                    .OrderByDescending(d => d.CreatedAt.ToDateTime())
                    .ToList();

                var totalCount = orderedCases.Count;

                if (!string.IsNullOrEmpty(startAfter))
                {
                    var index = orderedCases.FindIndex(c => c.Id == startAfter);
                    if (index >= 0)
                        orderedCases = orderedCases.Skip(index + 1).ToList();
                }

                var items = orderedCases
                    .Take(limit)
                    .Select(MapCaseDoc)
                    .ToList();

                return new FraudCasePageDto { Items = items, TotalCount = totalCount };
            }
        }

        private static bool IsMissingCompositeIndex(RpcException ex)
        {
            return ex.StatusCode == StatusCode.FailedPrecondition &&
                   ex.Status.Detail.Contains("requires an index", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<FraudCaseDto?> GetCaseByIdAsync(string caseId)
        {
            var snapshot = await _firestore.FraudCases.Document(caseId).GetSnapshotAsync();
            if (!snapshot.Exists) return null;
            return MapCaseDoc(snapshot.ConvertTo<FraudCaseDocument>());
        }

        public async Task<FraudCaseDto?> ApproveCaseAsync(string caseId, string resolvedBy, string? notes = null, string? correlationId = null)
        {
            return await ResolveCaseAsync(caseId, FraudCaseStatus.Approved, resolvedBy, notes, correlationId);
        }

        public async Task<FraudCaseDto?> RejectCaseAsync(string caseId, string resolvedBy, string? notes = null, string? correlationId = null)
        {
            return await ResolveCaseAsync(caseId, FraudCaseStatus.Rejected, resolvedBy, notes, correlationId);
        }

        public async Task<FraudCaseDto?> EscalateCaseAsync(string caseId, string? notes = null, string? correlationId = null)
        {
            var docRef = _firestore.FraudCases.Document(caseId);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var doc = snapshot.ConvertTo<FraudCaseDocument>();
            var now = Timestamp.GetCurrentTimestamp();

            doc.Status = FraudCaseStatus.InReview.ToString();
            if (notes != null) doc.AnalystNotes = notes;
            doc.UpdatedAt = now;

            await docRef.SetAsync(doc, SetOptions.Overwrite);

            await _audit.LogAsync("system", "fraud_case_escalated", "FraudCase", caseId,
                new { previousStatus = doc.Status, notes }, correlationId);

            _logger.LogInformation(
                "Fraud case escalated. CaseId={CaseId}, CorrelationId={CorrelationId}",
                caseId, correlationId);

            return MapCaseDoc(doc);
        }

        public async Task<FraudCaseDto?> AssignCaseAsync(string caseId, string assignee, string? correlationId = null)
        {
            var docRef = _firestore.FraudCases.Document(caseId);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var doc = snapshot.ConvertTo<FraudCaseDocument>();
            var now = Timestamp.GetCurrentTimestamp();

            doc.Assignee = assignee;
            doc.Status = FraudCaseStatus.InReview.ToString();
            doc.UpdatedAt = now;

            await docRef.SetAsync(doc, SetOptions.Overwrite);

            await _audit.LogAsync("system", "fraud_case_assigned", "FraudCase", caseId,
                new { assignee }, correlationId);

            _logger.LogInformation(
                "Fraud case assigned. CaseId={CaseId}, Assignee={Assignee}, CorrelationId={CorrelationId}",
                caseId, assignee, correlationId);

            return MapCaseDoc(doc);
        }

        private async Task<FraudCaseDto?> ResolveCaseAsync(
            string caseId, FraudCaseStatus newStatus, string resolvedBy, string? notes, string? correlationId)
        {
            var docRef = _firestore.FraudCases.Document(caseId);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var doc = snapshot.ConvertTo<FraudCaseDocument>();
            var previousStatus = doc.Status;
            var now = Timestamp.GetCurrentTimestamp();

            doc.Status = newStatus.ToString();
            doc.ResolvedBy = resolvedBy;
            doc.ResolvedAt = now;
            doc.UpdatedAt = now;
            if (notes != null) doc.AnalystNotes = notes;

            await docRef.SetAsync(doc, SetOptions.Overwrite);

            var action = newStatus == FraudCaseStatus.Approved ? "fraud_case_approved" : "fraud_case_rejected";
            await _audit.LogAsync(resolvedBy, action, "FraudCase", caseId,
                new { previousStatus, newStatus = newStatus.ToString(), notes }, correlationId);

            _logger.LogInformation(
                "Fraud case resolved. CaseId={CaseId}, Status={Status}, ResolvedBy={ResolvedBy}, CorrelationId={CorrelationId}",
                caseId, newStatus, resolvedBy, correlationId);

            return MapCaseDoc(doc);
        }

        private static FraudCaseDto MapCaseDoc(FraudCaseDocument doc) => new()
        {
            Id = doc.Id,
            EvaluationId = doc.EvaluationId,
            UserId = doc.UserId,
            PaymentId = doc.PaymentId,
            Status = doc.Status,
            RiskLevel = doc.RiskLevel,
            FraudScore = doc.FraudScore,
            AmountMinorUnits = doc.AmountMinorUnits,
            Currency = doc.Currency,
            Assignee = doc.Assignee,
            Reasons = doc.Reasons,
            RulesTriggered = doc.RulesTriggered,
            AnalystNotes = doc.AnalystNotes,
            ResolvedBy = doc.ResolvedBy,
            ResolvedAt = doc.ResolvedAt?.ToDateTime(),
            CreatedAt = doc.CreatedAt.ToDateTime(),
            UpdatedAt = doc.UpdatedAt.ToDateTime()
        };
    }
}
