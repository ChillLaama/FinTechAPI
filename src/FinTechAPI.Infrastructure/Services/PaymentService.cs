using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Application.Utilities;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using FinTechAPI.Infrastructure.Payments;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinTechAPI.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly FirestoreProvider _firestore;
        private readonly StripeSettings _settings;
        private readonly IStripePaymentIntentService _stripeService;
        private readonly ITransactionService _transactionService;
        private readonly IFraudService _fraudService;
        private readonly IFraudCaseService _fraudCaseService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentService> _logger;

        // Defines the forward-moving lifecycle order. Terminal statuses share the
        // highest rank so they can replace any non-terminal state but not each other.
        private static readonly IReadOnlyDictionary<string, int> StatusRank =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["requires_payment_method"] = 0,
                ["requires_confirmation"] = 1,
                ["requires_action"] = 2,
                ["processing"] = 3,
                ["requires_capture"] = 4,
                ["succeeded"] = 5,
                ["canceled"] = 5,
            };

        private static readonly IReadOnlySet<string> TerminalStatuses =
            new HashSet<string>(["succeeded", "canceled"], StringComparer.OrdinalIgnoreCase);

        public PaymentService(
            FirestoreProvider firestore,
            IOptions<StripeSettings> settings,
            IStripePaymentIntentService stripeService,
            ITransactionService transactionService,
            IFraudService fraudService,
            IFraudCaseService fraudCaseService,
            INotificationService notificationService,
            ILogger<PaymentService> logger)
        {
            _firestore = firestore;
            _settings = settings.Value;
            _stripeService = stripeService;
            _transactionService = transactionService;
            _fraudService = fraudService;
            _fraudCaseService = fraudCaseService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
            CreatePaymentIntentDto dto, string userId, string idempotencyKey)
        {
            var amountMinorUnits = AmountConverter.ToMinorUnits(dto.Amount);
            var normalizedCurrency = dto.Currency.Trim().ToLowerInvariant();

            _logger.LogInformation(
                "Creating payment intent. UserId={UserId}, AmountMinorUnits={AmountMinorUnits}, Currency={Currency}, IdempotencyKey={IdempotencyKey}",
                userId,
                amountMinorUnits,
                normalizedCurrency,
                idempotencyKey);

            // ── Fraud pre-check ──────────────────────────────────────────
            var fraudResult = await _fraudService.EvaluateAsync(
                userId, amountMinorUnits, normalizedCurrency,
                paymentId: null, transactionId: dto.TransactionId);

            if (string.Equals(fraudResult.Decision, "Block", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Payment blocked by fraud check. UserId={UserId}, FraudScore={FraudScore}, EvaluationId={EvaluationId}",
                    userId, fraudResult.FraudScore, fraudResult.EvaluationId);

                await _notificationService.SendAsync(userId, "fraud_block",
                    "Payment blocked",
                    $"Your payment of {dto.Amount} {normalizedCurrency.ToUpperInvariant()} was blocked by our security system. Contact support if you believe this is an error.",
                    "payment", null);

                throw new PaymentProviderException(
                    "Payment blocked by fraud detection. Please contact support.");
            }

            if (string.Equals(fraudResult.Decision, "Review", StringComparison.OrdinalIgnoreCase))
            {
                await _notificationService.SendAsync(userId, "fraud_review",
                    "Payment under review",
                    $"Your payment of {dto.Amount} {normalizedCurrency.ToUpperInvariant()} is being reviewed by our security team.",
                    "payment", null);

                await _fraudCaseService.CreateCaseAsync(
                    fraudResult.EvaluationId, userId, null,
                    fraudResult.RiskLevel, fraudResult.FraudScore,
                    amountMinorUnits, normalizedCurrency,
                    fraudResult.Reasons, fraudResult.RulesTriggered,
                    fraudResult.MlAnomalyScore, fraudResult.MlModelVersion);
            }

            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = amountMinorUnits,
                Currency = normalizedCurrency,
                Description = dto.Description,
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["transactionId"] = dto.TransactionId ?? string.Empty
                }
            };

            var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };

            PaymentIntent intent;
            try
            {
                intent = await _stripeService.CreateAsync(createOptions, requestOptions);

                _logger.LogInformation(
                    "Stripe payment intent created. StripePaymentIntentId={StripePaymentIntentId}, UserId={UserId}, Status={Status}",
                    intent.Id,
                    userId,
                    intent.Status);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe create payment intent failed. UserId={UserId}, StripeCode={StripeCode}",
                    userId,
                    ex.StripeError?.Code);

                throw new PaymentProviderException(
                    $"Stripe rejected the payment intent: {ex.StripeError?.Message ?? ex.Message}",
                    ex,
                    ex.StripeError?.Code);
            }

            var now = Timestamp.GetCurrentTimestamp();
            var paymentDocRef = _firestore.Payments.Document();
            var paymentDoc = new PaymentDocument
            {
                Id = paymentDocRef.Id,
                UserId = userId,
                AmountMinorUnits = amountMinorUnits,
                Currency = normalizedCurrency,
                Status = intent.Status,
                StripePaymentIntentId = intent.Id,
                TransactionId = dto.TransactionId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await paymentDocRef.SetAsync(paymentDoc);

            _logger.LogInformation(
                "Payment document persisted. PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}, UserId={UserId}, Status={Status}",
                paymentDoc.Id,
                intent.Id,
                userId,
                intent.Status);

            return new PaymentIntentResponseDto
            {
                PaymentId = paymentDoc.Id,
                StripePaymentIntentId = intent.Id,
                ClientSecret = intent.ClientSecret,
                Status = intent.Status,
                Amount = dto.Amount,
                Currency = normalizedCurrency,
                TransactionId = dto.TransactionId,
                FraudDecision = fraudResult.Decision,
                FraudScore = fraudResult.FraudScore,
                FraudEvaluationId = fraudResult.EvaluationId,
            };
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(string paymentId, string userId)
        {
            var snapshot = await _firestore.Payments.Document(paymentId).GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                _logger.LogWarning("Payment not found. PaymentId={PaymentId}, UserId={UserId}", paymentId, userId);
                return null;
            }

            var paymentDoc = snapshot.ConvertTo<PaymentDocument>();

            // Ownership check — prevents IDOR
            if (!string.Equals(paymentDoc.UserId, userId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Payment access denied by ownership check. PaymentId={PaymentId}, RequestedUserId={RequestedUserId}, OwnerUserId={OwnerUserId}",
                    paymentId,
                    userId,
                    paymentDoc.UserId);
                return null;
            }

            _logger.LogInformation(
                "Payment loaded. PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}, UserId={UserId}, Status={Status}",
                paymentDoc.Id,
                paymentDoc.StripePaymentIntentId,
                paymentDoc.UserId,
                paymentDoc.Status);

            return ToDto(paymentDoc);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByUserIdAsync(string userId)
        {
            var snapshot = await _firestore.Payments
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<PaymentDocument>())
                .Select(ToDto)
                .ToList();
        }

        public async Task<PaymentDto?> ReconcilePaymentAsync(string paymentId, string userId)
        {
            var snapshot = await _firestore.Payments.Document(paymentId).GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                _logger.LogWarning("Manual reconciliation requested for missing payment. PaymentId={PaymentId}, UserId={UserId}", paymentId, userId);
                return null;
            }

            var paymentDoc = snapshot.ConvertTo<PaymentDocument>();
            if (!string.Equals(paymentDoc.UserId, userId, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Manual reconciliation denied by ownership check. PaymentId={PaymentId}, RequestedUserId={RequestedUserId}, OwnerUserId={OwnerUserId}",
                    paymentId,
                    userId,
                    paymentDoc.UserId);
                return null;
            }

            PaymentIntent intent;
            try
            {
                intent = await _stripeService.GetAsync(paymentDoc.StripePaymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe payment intent retrieval failed during manual reconciliation. PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}, UserId={UserId}, StripeCode={StripeCode}",
                    paymentId,
                    paymentDoc.StripePaymentIntentId,
                    userId,
                    ex.StripeError?.Code);

                throw new PaymentProviderException(
                    "Stripe reconciliation request failed.",
                    ex,
                    ex.StripeError?.Code);
            }

            var previousStatus = paymentDoc.Status;
            paymentDoc.Status = intent.Status ?? paymentDoc.Status;
            paymentDoc.LastWebhookEvent = "manual_reconcile";
            paymentDoc.LastStripeEventId = $"manual-reconcile:{DateTime.UtcNow:O}";
            paymentDoc.UpdatedAt = Timestamp.GetCurrentTimestamp();

            await _firestore.Payments.Document(paymentId).SetAsync(paymentDoc, SetOptions.Overwrite);

            _logger.LogInformation(
                "Manual reconciliation applied. PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}, UserId={UserId}",
                paymentId,
                paymentDoc.StripePaymentIntentId,
                previousStatus,
                paymentDoc.Status,
                userId);

            await SyncTransactionStatusAsync(paymentDoc.TransactionId, userId, paymentDoc.Status, "manual_reconcile");

            return ToDto(paymentDoc);
        }

        public async Task<bool> HandleStripeWebhookAsync(string payload, string signatureHeader)
        {
            EnsureWebhookSecretConfigured();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    payload, signatureHeader, _settings.WebhookSecret);

                _logger.LogInformation(
                    "Stripe webhook verified. EventId={EventId}, EventType={EventType}",
                    stripeEvent.Id,
                    stripeEvent.Type);
            }
            catch (StripeException)
            {
                _logger.LogWarning("Stripe webhook signature verification failed.");
                return false;
            }

            if (stripeEvent.Data?.Object is not PaymentIntent intent)
            {
                _logger.LogInformation(
                    "Ignoring non-payment-intent webhook event. EventId={EventId}, EventType={EventType}",
                    stripeEvent.Id,
                    stripeEvent.Type);
                return true; // Not a PaymentIntent event — acknowledge silently
            }

            var querySnapshot = await _firestore.Payments
                .WhereEqualTo("stripePaymentIntentId", intent.Id)
                .Limit(1)
                .GetSnapshotAsync();

            var paymentDocument = querySnapshot.Documents.FirstOrDefault();
            if (paymentDocument is null)
            {
                _logger.LogWarning(
                    "Webhook references unknown payment intent. EventId={EventId}, EventType={EventType}, StripePaymentIntentId={StripePaymentIntentId}",
                    stripeEvent.Id,
                    stripeEvent.Type,
                    intent.Id);
                return true; // Unknown payment — acknowledge to stop retries
            }

            var paymentId = paymentDocument.Id;

            var currentStatus = paymentDocument.GetValue<string>("status");
            var lastStripeEventId = paymentDocument.ContainsField("lastStripeEventId")
                                      ? paymentDocument.GetValue<string>("lastStripeEventId")
                                      : null;
            var userId = paymentDocument.ContainsField("userId")
                ? paymentDocument.GetValue<string>("userId")
                : string.Empty;

            // ── Idempotency: skip already-processed events ───────────────────
            if (!string.IsNullOrEmpty(lastStripeEventId) &&
                string.Equals(lastStripeEventId, stripeEvent.Id, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Duplicate webhook event ignored. EventId={EventId}, PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}",
                    stripeEvent.Id,
                    paymentId,
                    intent.Id);
                return true;
            }

            // ── State-machine guard: never move backward, never leave terminal ─
            var isCurrentTerminal = TerminalStatuses.Contains(currentStatus ?? string.Empty);
            var currentRank = StatusRank.GetValueOrDefault(currentStatus ?? string.Empty, -1);
            var newRank = StatusRank.GetValueOrDefault(intent.Status ?? string.Empty, -1);

            if (isCurrentTerminal || newRank < currentRank)
            {
                _logger.LogWarning(
                    "Webhook status transition rejected. PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}, CurrentStatus={CurrentStatus}, NewStatus={NewStatus}, EventId={EventId}, EventType={EventType}, UserId={UserId}",
                    paymentId,
                    intent.Id,
                    currentStatus,
                    intent.Status,
                    stripeEvent.Id,
                    stripeEvent.Type,
                    userId);
                return true; // Reject out-of-order or post-terminal update
            }

            await paymentDocument.Reference.UpdateAsync(new Dictionary<string, object>
            {
                ["status"] = intent.Status ?? string.Empty,
                ["lastWebhookEvent"] = stripeEvent.Type ?? string.Empty,
                ["lastStripeEventId"] = stripeEvent.Id ?? string.Empty,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp(),
            });

            _logger.LogInformation(
                "Payment status updated from webhook. PaymentId={PaymentId}, StripePaymentIntentId={StripePaymentIntentId}, PreviousStatus={PreviousStatus}, NewStatus={NewStatus}, EventId={EventId}, EventType={EventType}, UserId={UserId}",
                paymentId,
                intent.Id,
                currentStatus,
                intent.Status,
                stripeEvent.Id,
                stripeEvent.Type,
                userId);

            var linkedTransactionId = paymentDocument.ContainsField("transactionId")
                ? paymentDocument.GetValue<string>("transactionId")
                : null;

            // ── Notify user about terminal status changes ────────────────
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(intent.Status))
            {
                var amountMinor = paymentDocument.ContainsField("amountMinorUnits")
                    ? paymentDocument.GetValue<long>("amountMinorUnits")
                    : 0;
                var currency = paymentDocument.ContainsField("currency")
                    ? paymentDocument.GetValue<string>("currency")?.ToUpperInvariant() ?? ""
                    : "";
                var displayAmount = amountMinor / 100m;

                if (string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    await _notificationService.SendAsync(userId, "payment_succeeded",
                        "Payment successful",
                        $"Your payment of {displayAmount:F2} {currency} has been processed successfully.",
                        "payment", paymentId);
                }
                else if (string.Equals(intent.Status, "canceled", StringComparison.OrdinalIgnoreCase))
                {
                    await _notificationService.SendAsync(userId, "payment_failed",
                        "Payment canceled",
                        $"Your payment of {displayAmount:F2} {currency} was canceled.",
                        "payment", paymentId);
                }
            }

            await SyncTransactionStatusAsync(linkedTransactionId, userId, intent.Status, stripeEvent.Type ?? "webhook");

            return true;
        }

        private async Task SyncTransactionStatusAsync(string? transactionId, string userId, string? providerStatus, string source)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(userId))
                return;

            var mappedStatus = MapProviderStatusToBusinessStatus(providerStatus);
            if (mappedStatus is null)
                return;

            var updatedTransaction = await _transactionService.UpdateTransactionStatusAsync(transactionId, mappedStatus.Value, userId);
            if (updatedTransaction is null)
            {
                _logger.LogWarning(
                    "Transaction sync skipped. TransactionId={TransactionId}, UserId={UserId}, ProviderStatus={ProviderStatus}, Source={Source}",
                    transactionId,
                    userId,
                    providerStatus,
                    source);
                return;
            }

            _logger.LogInformation(
                "Transaction status synced from provider. TransactionId={TransactionId}, UserId={UserId}, BusinessStatus={BusinessStatus}, ProviderStatus={ProviderStatus}, Source={Source}",
                transactionId,
                userId,
                mappedStatus,
                providerStatus,
                source);
        }

        private static Domain.Models.TransactionStatus? MapProviderStatusToBusinessStatus(string? providerStatus)
        {
            if (string.IsNullOrWhiteSpace(providerStatus))
                return null;

            return providerStatus.ToLowerInvariant() switch
            {
                "succeeded" => Domain.Models.TransactionStatus.Succeeded,
                "canceled" => Domain.Models.TransactionStatus.Failed,
                "requires_payment_method" => Domain.Models.TransactionStatus.Failed,
                "processing" => Domain.Models.TransactionStatus.Pending,
                "requires_confirmation" => Domain.Models.TransactionStatus.Pending,
                "requires_action" => Domain.Models.TransactionStatus.Pending,
                "requires_capture" => Domain.Models.TransactionStatus.Pending,
                _ => null,
            };
        }

        private void EnsureWebhookSecretConfigured()
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
                throw new PaymentConfigurationException("Stripe:WebhookSecret is not configured.");
        }

        private static PaymentDto ToDto(PaymentDocument document) =>
            new()
            {
                Id = document.Id,
                UserId = document.UserId,
                Amount = AmountConverter.FromMinorUnits(document.AmountMinorUnits),
                Currency = document.Currency,
                Status = document.Status,
                StripePaymentIntentId = document.StripePaymentIntentId,
                TransactionId = document.TransactionId,
                LastWebhookEvent = document.LastWebhookEvent,
                LastStripeEventId = document.LastStripeEventId,
                CreatedAt = document.CreatedAt.ToDateTime(),
                UpdatedAt = document.UpdatedAt.ToDateTime(),
            };

        public async Task<IReadOnlyList<PendingPaymentDto>> GetPendingPaymentsForAdminAsync(int staleAfterMinutes = 5, int limit = 100)
        {
            var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-staleAfterMinutes));

            var snapshot = await _firestore.Payments
                .WhereLessThan("updatedAt", cutoff)
                .Limit(limit)
                .GetSnapshotAsync();

            var now = DateTime.UtcNow;

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<PaymentDocument>())
                .Where(p => !TerminalStatuses.Contains(p.Status))
                .OrderBy(p => p.UpdatedAt)
                .Select(p => new PendingPaymentDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Amount = AmountConverter.FromMinorUnits(p.AmountMinorUnits),
                    Currency = p.Currency,
                    Status = p.Status,
                    StripePaymentIntentId = p.StripePaymentIntentId,
                    LastWebhookEvent = p.LastWebhookEvent,
                    CreatedAt = p.CreatedAt.ToDateTime(),
                    UpdatedAt = p.UpdatedAt.ToDateTime(),
                    StaleMinutes = (int)(now - p.UpdatedAt.ToDateTime()).TotalMinutes
                })
                .ToList();
        }
    }
}
