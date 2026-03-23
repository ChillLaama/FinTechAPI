using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace FinTechAPI.Infrastructure.Services
{
    public class FraudRuleEngine : IFraudService
    {
        private readonly FirestoreProvider _firestore;
        private readonly ILogger<FraudRuleEngine> _logger;
        private const string RulesVersion = "1.0";

        // Rule thresholds
        private const int VelocityWindowMinutes = 60;
        private const int VelocityMaxPayments = 5;
        private const int VelocityScoreContribution = 30;

        private const double AmountAnomalyMultiplier = 3.0;
        private const int AmountAnomalyScoreContribution = 25;

        private const int RepeatedFailureWindowMinutes = 30;
        private const int RepeatedFailureMaxCount = 3;
        private const int RepeatedFailureScoreContribution = 20;

        private const long HighAmountThresholdMinorUnits = 500_000; // $5,000
        private const int HighAmountScoreContribution = 15;

        private const long VeryHighAmountThresholdMinorUnits = 1_000_000; // $10,000
        private const int VeryHighAmountScoreContribution = 25;

        // Score → decision thresholds
        private const int ReviewThreshold = 40;
        private const int BlockThreshold = 75;

        public FraudRuleEngine(FirestoreProvider firestore, ILogger<FraudRuleEngine> logger)
        {
            _firestore = firestore;
            _logger = logger;
        }

        public async Task<FraudCheckResultDto> EvaluateAsync(
            string userId, long amountMinorUnits, string currency,
            string? paymentId = null, string? transactionId = null, string? correlationId = null)
        {
            var reasons = new List<string>();
            var rulesTriggered = new List<string>();
            var totalScore = 0;

            // ── Rule 1: Velocity ─────────────────────────────────────
            var velocityScore = await EvaluateVelocityRuleAsync(userId, reasons, rulesTriggered);
            totalScore += velocityScore;

            // ── Rule 2: Amount anomaly ───────────────────────────────
            var anomalyScore = await EvaluateAmountAnomalyRuleAsync(userId, amountMinorUnits, reasons, rulesTriggered);
            totalScore += anomalyScore;

            // ── Rule 3: Repeated failures ────────────────────────────
            var failureScore = await EvaluateRepeatedFailureRuleAsync(userId, reasons, rulesTriggered);
            totalScore += failureScore;

            // ── Rule 4: High amount ──────────────────────────────────
            var highAmountScore = EvaluateHighAmountRule(amountMinorUnits, reasons, rulesTriggered);
            totalScore += highAmountScore;

            // Cap at 100
            totalScore = Math.Min(totalScore, 100);

            // Determine risk level and decision
            var riskLevel = totalScore switch
            {
                >= 75 => RiskLevel.Critical,
                >= 50 => RiskLevel.High,
                >= 25 => RiskLevel.Medium,
                _ => RiskLevel.Low
            };

            var decision = totalScore switch
            {
                >= BlockThreshold => FraudDecision.Block,
                >= ReviewThreshold => FraudDecision.Review,
                _ => FraudDecision.Allow
            };

            // Shadow mode: for Block decisions, log but downgrade to Review
            // This allows monitoring without blocking legitimate users during initial rollout
            if (decision == FraudDecision.Block)
            {
                _logger.LogWarning(
                    "Fraud shadow-mode: would BLOCK payment. UserId={UserId}, Score={FraudScore}, Rules={RulesTriggered}, CorrelationId={CorrelationId}",
                    userId, totalScore, string.Join(",", rulesTriggered), correlationId);

                decision = FraudDecision.Review;
                reasons.Add("Shadow mode: block downgraded to review");
            }

            // Persist evaluation
            var evalDocRef = _firestore.FraudEvaluations.Document();
            var now = Timestamp.GetCurrentTimestamp();
            var evalDoc = new FraudEvaluationDocument
            {
                Id = evalDocRef.Id,
                UserId = userId,
                PaymentId = paymentId,
                TransactionId = transactionId,
                FraudScore = totalScore,
                RiskLevel = riskLevel.ToString(),
                Decision = decision.ToString(),
                Reasons = reasons,
                RulesTriggered = rulesTriggered,
                RulesVersion = RulesVersion,
                CorrelationId = correlationId,
                AmountMinorUnits = amountMinorUnits,
                Currency = currency,
                CreatedAt = now
            };

            await evalDocRef.SetAsync(evalDoc);

            _logger.LogInformation(
                "Fraud evaluation completed. EvaluationId={EvaluationId}, UserId={UserId}, Score={FraudScore}, Decision={Decision}, RiskLevel={RiskLevel}, CorrelationId={CorrelationId}",
                evalDoc.Id, userId, totalScore, decision, riskLevel, correlationId);

            return new FraudCheckResultDto
            {
                EvaluationId = evalDoc.Id,
                FraudScore = totalScore,
                RiskLevel = riskLevel.ToString(),
                Decision = decision.ToString(),
                Reasons = reasons,
                RulesTriggered = rulesTriggered
            };
        }

        public async Task<FraudEvaluationDto?> GetEvaluationByIdAsync(string evaluationId)
        {
            var snapshot = await _firestore.FraudEvaluations.Document(evaluationId).GetSnapshotAsync();
            if (!snapshot.Exists) return null;
            return MapEvaluationDoc(snapshot.ConvertTo<FraudEvaluationDocument>());
        }

        public async Task<IEnumerable<FraudEvaluationDto>> GetEvaluationsByUserIdAsync(string userId)
        {
            var query = _firestore.FraudEvaluations
                .WhereEqualTo("userId", userId)
                .OrderByDescending("createdAt")
                .Limit(50);

            var snapshots = await query.GetSnapshotAsync();
            return snapshots.Documents
                .Select(d => MapEvaluationDoc(d.ConvertTo<FraudEvaluationDocument>()))
                .ToList();
        }

        // ── Rule implementations ──────────────────────────────────────────

        private async Task<int> EvaluateVelocityRuleAsync(string userId, List<string> reasons, List<string> rulesTriggered)
        {
            var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-VelocityWindowMinutes));
            var recentPayments = await _firestore.Payments
                .WhereEqualTo("userId", userId)
                .WhereGreaterThanOrEqualTo("createdAt", cutoff)
                .GetSnapshotAsync();

            if (recentPayments.Count >= VelocityMaxPayments)
            {
                rulesTriggered.Add("velocity");
                reasons.Add($"High velocity: {recentPayments.Count} payments in last {VelocityWindowMinutes} minutes (threshold: {VelocityMaxPayments})");
                return VelocityScoreContribution;
            }

            return 0;
        }

        private async Task<int> EvaluateAmountAnomalyRuleAsync(string userId, long amountMinorUnits, List<string> reasons, List<string> rulesTriggered)
        {
            // Get the user's recent payments to compute average
            var recentPayments = await _firestore.Payments
                .WhereEqualTo("userId", userId)
                .OrderByDescending("createdAt")
                .Limit(20)
                .GetSnapshotAsync();

            if (recentPayments.Count < 3) return 0; // Not enough history

            var amounts = recentPayments.Documents
                .Select(d => d.ConvertTo<PaymentDocument>().AmountMinorUnits)
                .ToList();

            var average = amounts.Average();
            if (average <= 0) return 0;

            var ratio = amountMinorUnits / average;
            if (ratio >= AmountAnomalyMultiplier)
            {
                rulesTriggered.Add("amount_anomaly");
                reasons.Add($"Amount anomaly: {amountMinorUnits} is {ratio:F1}x user average ({average:F0})");
                return AmountAnomalyScoreContribution;
            }

            return 0;
        }

        private async Task<int> EvaluateRepeatedFailureRuleAsync(string userId, List<string> reasons, List<string> rulesTriggered)
        {
            var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-RepeatedFailureWindowMinutes));
            var recentPayments = await _firestore.Payments
                .WhereEqualTo("userId", userId)
                .WhereGreaterThanOrEqualTo("createdAt", cutoff)
                .GetSnapshotAsync();

            var failedCount = recentPayments.Documents
                .Select(d => d.ConvertTo<PaymentDocument>())
                .Count(p => string.Equals(p.Status, "canceled", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(p.Status, "requires_payment_method", StringComparison.OrdinalIgnoreCase));

            if (failedCount >= RepeatedFailureMaxCount)
            {
                rulesTriggered.Add("repeated_failure");
                reasons.Add($"Repeated failures: {failedCount} failed payments in last {RepeatedFailureWindowMinutes} minutes");
                return RepeatedFailureScoreContribution;
            }

            return 0;
        }

        private static int EvaluateHighAmountRule(long amountMinorUnits, List<string> reasons, List<string> rulesTriggered)
        {
            if (amountMinorUnits >= VeryHighAmountThresholdMinorUnits)
            {
                rulesTriggered.Add("very_high_amount");
                reasons.Add($"Very high amount: {amountMinorUnits} minor units (threshold: {VeryHighAmountThresholdMinorUnits})");
                return VeryHighAmountScoreContribution;
            }

            if (amountMinorUnits >= HighAmountThresholdMinorUnits)
            {
                rulesTriggered.Add("high_amount");
                reasons.Add($"High amount: {amountMinorUnits} minor units (threshold: {HighAmountThresholdMinorUnits})");
                return HighAmountScoreContribution;
            }

            return 0;
        }

        private static FraudEvaluationDto MapEvaluationDoc(FraudEvaluationDocument doc) => new()
        {
            Id = doc.Id,
            UserId = doc.UserId,
            PaymentId = doc.PaymentId,
            TransactionId = doc.TransactionId,
            FraudScore = doc.FraudScore,
            RiskLevel = doc.RiskLevel,
            Decision = doc.Decision,
            Reasons = doc.Reasons,
            RulesTriggered = doc.RulesTriggered,
            RulesVersion = doc.RulesVersion,
            AmountMinorUnits = doc.AmountMinorUnits,
            Currency = doc.Currency,
            CreatedAt = doc.CreatedAt.ToDateTime()
        };
    }
}
