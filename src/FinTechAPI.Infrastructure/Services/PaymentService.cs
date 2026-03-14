using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
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

        public PaymentService(FirestoreProvider firestore, IOptions<StripeSettings> settings)
        {
            _firestore = firestore;
            _settings = settings.Value;
        }

        public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto, string userId, string idempotencyKey)
        {
            EnsureApiKeyConfigured();
            StripeConfiguration.ApiKey = _settings.ApiKey;

            var amountInCents = Convert.ToInt64(decimal.Round(dto.Amount * 100m, 0, MidpointRounding.AwayFromZero));
            var normalizedCurrency = dto.Currency.Trim().ToLowerInvariant();

            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = normalizedCurrency,
                Description = dto.Description,
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["transactionId"] = dto.TransactionId ?? string.Empty
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var stripeService = new Stripe.PaymentIntentService();
            var intent = await stripeService.CreateAsync(createOptions, requestOptions);

            var now = Timestamp.GetCurrentTimestamp();
            var paymentDocRef = _firestore.Payments.Document();
            var paymentDoc = new PaymentDocument
            {
                Id = paymentDocRef.Id,
                UserId = userId,
                Amount = (double)dto.Amount,
                Currency = normalizedCurrency,
                Status = intent.Status,
                StripePaymentIntentId = intent.Id,
                TransactionId = dto.TransactionId,
                CreatedAt = now,
                UpdatedAt = now
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
                TransactionId = dto.TransactionId
            };
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(string paymentId, string userId)
        {
            var snapshot = await _firestore.Payments.Document(paymentId).GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;

            var paymentDoc = snapshot.ConvertTo<PaymentDocument>();
            if (paymentDoc.UserId != userId)
                return null;

            return ToDto(paymentDoc);
        }

        public async Task<bool> HandleStripeWebhookAsync(string payload, string signatureHeader)
        {
            EnsureWebhookSecretConfigured();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _settings.WebhookSecret);
            }
            catch (StripeException)
            {
                return false;
            }

            if (stripeEvent.Data?.Object is not PaymentIntent intent)
                return true;

            var snapshot = await _firestore.Payments
                .WhereEqualTo("stripePaymentIntentId", intent.Id)
                .Limit(1)
                .GetSnapshotAsync();

            var paymentDocument = snapshot.Documents.FirstOrDefault();
            if (paymentDocument == null)
                return true;

            await paymentDocument.Reference.UpdateAsync(new Dictionary<string, object>
            {
                ["status"] = intent.Status,
                ["lastWebhookEvent"] = stripeEvent.Type,
                ["updatedAt"] = Timestamp.GetCurrentTimestamp()
            });

            return true;
        }

        private void EnsureApiKeyConfigured()
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
                throw new InvalidOperationException("Stripe ApiKey is not configured.");
        }

        private void EnsureWebhookSecretConfigured()
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
                throw new InvalidOperationException("Stripe WebhookSecret is not configured.");
        }

        private static PaymentDto ToDto(PaymentDocument document)
        {
            return new PaymentDto
            {
                Id = document.Id,
                UserId = document.UserId,
                Amount = (decimal)document.Amount,
                Currency = document.Currency,
                Status = document.Status,
                StripePaymentIntentId = document.StripePaymentIntentId,
                TransactionId = document.TransactionId,
                CreatedAt = document.CreatedAt.ToDateTime(),
                UpdatedAt = document.UpdatedAt.ToDateTime()
            };
        }
    }
}
