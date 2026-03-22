using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;

namespace FinTechAPI.Infrastructure.Services
{
    public class PlatformSummaryService : IPlatformSummaryService
    {
        private static readonly HashSet<string> FailedStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "failed",
                "canceled",
                "requires_payment_method",
            };

        private readonly FirestoreProvider _firestore;

        public PlatformSummaryService(FirestoreProvider firestore)
        {
            _firestore = firestore;
        }

        public async Task<PlatformSummaryDto> GetPlatformSummaryAsync(
            string userId,
            string currency,
            CancellationToken cancellationToken = default)
        {
            var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
                ? "usd"
                : currency.Trim().ToLowerInvariant();

            var paymentsSnapshot = await _firestore.Payments
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("currency", normalizedCurrency)
                .GetSnapshotAsync();

            var paymentDocs = paymentsSnapshot.Documents
                .Select(doc => doc.ConvertTo<PaymentDocument>())
                .ToList();

            var transactionsSnapshot = await _firestore.Transactions
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            var transactionDocs = transactionsSnapshot.Documents
                .Select(doc => doc.ConvertTo<TransactionDocument>())
                .ToList();

            var successfulPayments = paymentDocs.Count(doc =>
                string.Equals(doc.Status, "succeeded", StringComparison.OrdinalIgnoreCase));

            var failedPayments = paymentDocs.Count(doc =>
                FailedStatuses.Contains(doc.Status));

            var processedVolumeMinor = paymentDocs
                .Where(doc => string.Equals(doc.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                .Sum(doc => doc.AmountMinorUnits);

            // Pending status is currently the closest available signal for review backlog.
            var pendingReviewCount = transactionDocs.Count(doc => doc.Status == (int)TransactionStatus.Pending);

            return new PlatformSummaryDto
            {
                ProcessedVolume = processedVolumeMinor / 100m,
                SuccessfulPayments = successfulPayments,
                FailedPayments = failedPayments,
                PendingReviewCount = pendingReviewCount,
                FraudBlockedCount = 0,
                Currency = normalizedCurrency,
                Source = "fintechapi+stripe",
                SyncedAt = DateTime.UtcNow
            };
        }
    }
}