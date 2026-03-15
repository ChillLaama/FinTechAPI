using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Application.Utilities;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Firebase.Documents;
using FinTechAPI.Infrastructure.Payments;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinTechAPI.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly FirestoreProvider _firestore;
        private readonly StripeSettings _settings;
        private readonly IStripePaymentIntentService _stripeService;

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
            IStripePaymentIntentService stripeService)
        {
            _firestore = firestore;
            _settings = settings.Value;
            _stripeService = stripeService;
        }

        public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
            CreatePaymentIntentDto dto, string userId, string idempotencyKey)
        {
            var amountMinorUnits = AmountConverter.ToMinorUnits(dto.Amount);
            var normalizedCurrency = dto.Currency.Trim().ToLowerInvariant();

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
            }
            catch (StripeException ex)
            {
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
                return null;

            var paymentDoc = snapshot.ConvertTo<PaymentDocument>();

            // Ownership check — prevents IDOR
            if (!string.Equals(paymentDoc.UserId, userId, StringComparison.Ordinal))
                return null;

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
            }
            catch (StripeException)
            {
                return false;
            }

            if (stripeEvent.Data?.Object is not PaymentIntent intent)
                return true; // Not a PaymentIntent event — acknowledge silently

            var querySnapshot = await _firestore.Payments
                .WhereEqualTo("stripePaymentIntentId", intent.Id)
                .Limit(1)
                .GetSnapshotAsync();

            var paymentDocument = querySnapshot.Documents.FirstOrDefault();
            if (paymentDocument is null)
                return true; // Unknown payment — acknowledge to stop retries

            var currentStatus = paymentDocument.GetValue<string>("status");
            var lastStripeEventId = paymentDocument.ContainsField("lastStripeEventId")
                                      ? paymentDocument.GetValue<string>("lastStripeEventId")
                                      : null;

            // ── Idempotency: skip already-processed events ───────────────────
            if (!string.IsNullOrEmpty(lastStripeEventId) &&
                string.Equals(lastStripeEventId, stripeEvent.Id, StringComparison.Ordinal))
            {
                return true;
            }

            // ── State-machine guard: never move backward, never leave terminal ─
            var isCurrentTerminal = TerminalStatuses.Contains(currentStatus ?? string.Empty);
            var currentRank = StatusRank.GetValueOrDefault(currentStatus ?? string.Empty, -1);
            var newRank = StatusRank.GetValueOrDefault(intent.Status ?? string.Empty, -1);

            if (isCurrentTerminal || newRank < currentRank)
                return true; // Reject out-of-order or post-terminal update

            await paymentDocument.Reference.UpdateAsync(new Dictionary<string, object>
            {
                ["status"] = (object)(intent.Status ?? string.Empty),
                ["lastWebhookEvent"] = (object)(stripeEvent.Type ?? string.Empty),
                ["lastStripeEventId"] = (object)(stripeEvent.Id ?? string.Empty),
                ["updatedAt"] = Timestamp.GetCurrentTimestamp(),
            });

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
