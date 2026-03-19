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
            ILogger<PaymentService> logger)
        {
            _firestore = firestore;
            _settings = settings.Value;
            _stripeService = stripeService;
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
                ["status"] = (object)(intent.Status ?? string.Empty),
                ["lastWebhookEvent"] = (object)(stripeEvent.Type ?? string.Empty),
                ["lastStripeEventId"] = (object)(stripeEvent.Id ?? string.Empty),
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

            return true;
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
                CreatedAt = document.CreatedAt.ToDateTime(),
                UpdatedAt = document.UpdatedAt.ToDateTime(),
            };
    }
}
