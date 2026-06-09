using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using FinTechAPI.Infrastructure.Payments;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinTechAPI.Infrastructure.Services
{
    public class ReconciliationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReconciliationBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        // Non-terminal payment statuses that need reconciliation
        private static readonly HashSet<string> PendingPaymentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "requires_payment_method", "requires_confirmation", "requires_action",
            "processing", "requires_capture"
        };

        // Terminal payout statuses — skip these
        private static readonly HashSet<string> TerminalPayoutStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "paid", "failed", "canceled"
        };

        public ReconciliationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReconciliationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reconciliation background service started. Interval={Interval}min", _interval.TotalMinutes);

            // Delay initial run to let the app fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunReconciliationCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reconciliation cycle failed");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Reconciliation background service stopped");
        }

        private async Task RunReconciliationCycleAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var firestore = scope.ServiceProvider.GetRequiredService<FirestoreProvider>();
            var stripeSettings = scope.ServiceProvider.GetRequiredService<IOptions<StripeSettings>>().Value;
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var systemAlertService = scope.ServiceProvider.GetRequiredService<ISystemAlertService>();

            if (string.IsNullOrEmpty(stripeSettings.ApiKey))
            {
                _logger.LogWarning("Stripe API key not configured — skipping reconciliation");
                return;
            }

            var paymentsReconciled = await ReconcilePendingPaymentsAsync(firestore, systemAlertService, ct);
            var payoutsReconciled = await ReconcilePendingPayoutsAsync(firestore, systemAlertService, ct);

            if (paymentsReconciled > 0 || payoutsReconciled > 0)
            {
                _logger.LogInformation(
                    "Reconciliation cycle complete. Payments={PaymentsReconciled}, Payouts={PayoutsReconciled}",
                    paymentsReconciled, payoutsReconciled);

                await auditService.LogAsync("system", "Reconciliation.CycleCompleted", "System", null,
                    new { paymentsReconciled, payoutsReconciled });
            }
        }

        private async Task<int> ReconcilePendingPaymentsAsync(FirestoreProvider firestore, ISystemAlertService systemAlertService, CancellationToken ct)
        {
            // Query payments older than 2 minutes that aren't in terminal status
            var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-2));
            var snapshot = await firestore.Payments
                .WhereLessThan("updatedAt", cutoff)
                .Limit(50)
                .GetSnapshotAsync(ct);

            var reconciled = 0;
            var stripeService = new PaymentIntentService();

            foreach (var doc in snapshot.Documents)
            {
                ct.ThrowIfCancellationRequested();

                var payment = doc.ConvertTo<PaymentDocument>();
                if (!PendingPaymentStatuses.Contains(payment.Status))
                    continue;

                try
                {
                    var intent = await stripeService.GetAsync(payment.StripePaymentIntentId);
                    if (string.Equals(payment.Status, intent.Status, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var previousStatus = payment.Status;
                    payment.Status = intent.Status ?? payment.Status;
                    payment.LastWebhookEvent = "background_reconcile";
                    payment.LastStripeEventId = $"bg-reconcile:{DateTime.UtcNow:O}";
                    payment.UpdatedAt = Timestamp.GetCurrentTimestamp();

                    await firestore.Payments.Document(payment.Id).SetAsync(payment, SetOptions.Overwrite);
                    reconciled++;

                    _logger.LogInformation(
                        "Background reconciliation updated payment. PaymentId={PaymentId}, {PreviousStatus}→{NewStatus}",
                        payment.Id, previousStatus, payment.Status);

                    // Create system alert for significant status changes
                    if (string.Equals(intent.Status, "canceled", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(intent.Status, "requires_payment_method", StringComparison.OrdinalIgnoreCase))
                    {
                        await systemAlertService.CreateAsync(
                            "reconciliation_mismatch",
                            "Payment status mismatch resolved",
                            $"Payment {payment.Id} was stuck in '{previousStatus}' — reconciled to '{intent.Status}' via background check.",
                            "warning",
                            "payment", payment.Id);
                    }
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Stripe error during background reconciliation of payment {PaymentId}", payment.Id);
                }
            }

            return reconciled;
        }

        private async Task<int> ReconcilePendingPayoutsAsync(FirestoreProvider firestore, ISystemAlertService systemAlertService, CancellationToken ct)
        {
            var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-2));
            var snapshot = await firestore.Payouts
                .WhereLessThan("updatedAt", cutoff)
                .Limit(50)
                .GetSnapshotAsync(ct);

            var reconciled = 0;
            var stripePayoutService = new Stripe.PayoutService();

            foreach (var doc in snapshot.Documents)
            {
                ct.ThrowIfCancellationRequested();

                var payoutDoc = doc.ConvertTo<PayoutDocument>();
                if (TerminalPayoutStatuses.Contains(payoutDoc.Status))
                    continue;

                try
                {
                    var payout = await stripePayoutService.GetAsync(
                        payoutDoc.StripePayoutId,
                        requestOptions: new RequestOptions { StripeAccount = payoutDoc.StripeAccountId });

                    if (string.Equals(payoutDoc.Status, payout.Status, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var previousStatus = payoutDoc.Status;
                    payoutDoc.Status = payout.Status ?? payoutDoc.Status;
                    payoutDoc.FailureCode = payout.FailureCode;
                    payoutDoc.FailureMessage = payout.FailureMessage;
                    payoutDoc.UpdatedAt = Timestamp.GetCurrentTimestamp();

                    // Map reserve status
                    payoutDoc.ReserveStatus = payoutDoc.Status switch
                    {
                        "paid" => "consumed",
                        "failed" or "canceled" => "released",
                        _ => payoutDoc.ReserveStatus
                    };

                    await firestore.Payouts.Document(payoutDoc.Id).SetAsync(payoutDoc, SetOptions.Overwrite);

                    // Also update the reserve document
                    if (!string.IsNullOrEmpty(payoutDoc.ReserveId))
                    {
                        var reserveSnap = await firestore.PayoutReserves.Document(payoutDoc.ReserveId).GetSnapshotAsync(ct);
                        if (reserveSnap.Exists)
                        {
                            var reserve = reserveSnap.ConvertTo<PayoutReserveDocument>();
                            reserve.Status = payoutDoc.ReserveStatus;
                            reserve.UpdatedAt = Timestamp.GetCurrentTimestamp();
                            await firestore.PayoutReserves.Document(payoutDoc.ReserveId).SetAsync(reserve, SetOptions.Overwrite);
                        }
                    }

                    reconciled++;

                    _logger.LogInformation(
                        "Background reconciliation updated payout. PayoutId={PayoutId}, {PreviousStatus}→{NewStatus}",
                        payoutDoc.Id, previousStatus, payoutDoc.Status);

                    // Alert on failed payouts
                    if (string.Equals(payoutDoc.Status, "failed", StringComparison.OrdinalIgnoreCase))
                    {
                        await systemAlertService.CreateAsync(
                            "payout_failed",
                            "Payout failed during reconciliation",
                            $"Payout {payoutDoc.Id} failed during background reconciliation. Code: {payoutDoc.FailureCode ?? "unknown"}.",
                            "critical",
                            "payout", payoutDoc.Id);
                    }
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Stripe error during background reconciliation of payout {PayoutId}", payoutDoc.Id);
                }
            }

            return reconciled;
        }
    }
}
