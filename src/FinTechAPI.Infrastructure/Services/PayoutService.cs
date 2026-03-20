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
    public class PayoutService : IPayoutService
    {
        private readonly FirestoreProvider _firestore;
        private readonly IPlatformBalanceService _platformBalanceService;
        private readonly StripeSettings _settings;
        private readonly ILogger<PayoutService> _logger;
        private readonly PayoutServiceStripeAdapter? _stripePayoutService;

        private static readonly HashSet<string> TerminalStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "paid",
                "failed",
                "canceled"
            };

        public PayoutService(
            FirestoreProvider firestore,
            IPlatformBalanceService platformBalanceService,
            IOptions<StripeSettings> settings,
            ILogger<PayoutService> logger)
        {
            _firestore = firestore;
            _platformBalanceService = platformBalanceService;
            _settings = settings.Value;
            _logger = logger;

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                var client = new StripeClient(_settings.ApiKey);
                _stripePayoutService = new PayoutServiceStripeAdapter(client);
            }
        }

        public async Task<PayoutDto> CreatePayoutAsync(CreatePayoutDto dto, string userId, string idempotencyKey)
        {
            EnsureStripeConfigured();

            var amountMinorUnits = AmountConverter.ToMinorUnits(dto.Amount);
            if (amountMinorUnits <= 0)
                throw new ArgumentException("Amount must be positive.", nameof(dto.Amount));

            var currency = NormalizeCurrency(dto.Currency);
            var prefunding = await _platformBalanceService.GetPlatformBalanceAsync(currency);
            if (prefunding.Available < dto.Amount)
            {
                throw new ArgumentException(
                    $"Insufficient available balance. Available={prefunding.Available} {currency}, requested={dto.Amount} {currency}.",
                    nameof(dto.Amount));
            }

            var now = Timestamp.GetCurrentTimestamp();
            var payoutRef = _firestore.Payouts.Document();
            var reserveRef = _firestore.PayoutReserves.Document();

            var reserveDoc = new PayoutReserveDocument
            {
                Id = reserveRef.Id,
                UserId = userId,
                PayoutId = payoutRef.Id,
                AmountMinorUnits = amountMinorUnits,
                Currency = currency,
                Status = "reserved",
                Reason = "prefunding_ok",
                CreatedAt = now,
                UpdatedAt = now
            };

            await reserveRef.SetAsync(reserveDoc);

            Payout payout;
            try
            {
                payout = await _stripePayoutService!.CreateAsync(
                    new PayoutCreateOptions
                    {
                        Amount = amountMinorUnits,
                        Currency = currency,
                        Description = dto.Description,
                        Metadata = new Dictionary<string, string>
                        {
                            ["userId"] = userId,
                            ["reserveId"] = reserveRef.Id,
                            ["externalReference"] = dto.ExternalReference ?? string.Empty
                        }
                    },
                    new RequestOptions
                    {
                        IdempotencyKey = idempotencyKey,
                        StripeAccount = string.IsNullOrWhiteSpace(dto.StripeAccountId)
                            ? null
                            : dto.StripeAccountId
                    });
            }
            catch (StripeException ex)
            {
                _logger.LogError(
                    ex,
                    "Stripe payout creation failed. UserId={UserId}, StripeCode={StripeCode}",
                    userId,
                    ex.StripeError?.Code);

                reserveDoc.Status = "released";
                reserveDoc.Reason = "stripe_create_failed";
                reserveDoc.UpdatedAt = Timestamp.GetCurrentTimestamp();
                await reserveRef.SetAsync(reserveDoc, SetOptions.Overwrite);

                throw new PaymentProviderException(
                    $"Stripe rejected payout creation: {ex.StripeError?.Message ?? ex.Message}",
                    ex,
                    ex.StripeError?.Code);
            }

            var payoutDoc = new PayoutDocument
            {
                Id = payoutRef.Id,
                UserId = userId,
                AmountMinorUnits = amountMinorUnits,
                Currency = currency,
                Status = payout.Status ?? "pending",
                StripePayoutId = payout.Id,
                StripeAccountId = dto.StripeAccountId,
                ReserveId = reserveRef.Id,
                ReserveStatus = MapReserveStatus(payout.Status),
                FailureCode = payout.FailureCode,
                FailureMessage = payout.FailureMessage,
                ExternalReference = dto.ExternalReference,
                CreatedAt = now,
                UpdatedAt = now
            };

            await payoutRef.SetAsync(payoutDoc);

            reserveDoc.Status = payoutDoc.ReserveStatus;
            reserveDoc.Reason = payoutDoc.Status;
            reserveDoc.UpdatedAt = Timestamp.GetCurrentTimestamp();
            await reserveRef.SetAsync(reserveDoc, SetOptions.Overwrite);

            _logger.LogInformation(
                "Payout created. PayoutId={PayoutId}, StripePayoutId={StripePayoutId}, UserId={UserId}, Status={Status}, ReserveStatus={ReserveStatus}",
                payoutDoc.Id,
                payoutDoc.StripePayoutId,
                userId,
                payoutDoc.Status,
                payoutDoc.ReserveStatus);

            return ToDto(payoutDoc);
        }

        public async Task<PayoutDto?> GetPayoutByIdAsync(string payoutId, string userId)
        {
            var snapshot = await _firestore.Payouts.Document(payoutId).GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;

            var payoutDoc = snapshot.ConvertTo<PayoutDocument>();
            if (!string.Equals(payoutDoc.UserId, userId, StringComparison.Ordinal))
                return null;

            return ToDto(payoutDoc);
        }

        public async Task<IEnumerable<PayoutDto>> GetPayoutsByUserIdAsync(string userId)
        {
            var snapshot = await _firestore.Payouts
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<PayoutDocument>())
                .OrderByDescending(doc => doc.CreatedAt)
                .Select(ToDto)
                .ToList();
        }

        public async Task<PayoutDto?> ReconcilePayoutAsync(string payoutId, string userId)
        {
            EnsureStripeConfigured();

            var snapshot = await _firestore.Payouts.Document(payoutId).GetSnapshotAsync();
            if (!snapshot.Exists)
                return null;

            var payoutDoc = snapshot.ConvertTo<PayoutDocument>();
            if (!string.Equals(payoutDoc.UserId, userId, StringComparison.Ordinal))
                return null;

            if (!TerminalStatuses.Contains(payoutDoc.Status))
            {
                Payout payout;
                try
                {
                    payout = await _stripePayoutService!.GetAsync(
                        payoutDoc.StripePayoutId,
                        new RequestOptions { StripeAccount = payoutDoc.StripeAccountId });
                }
                catch (StripeException ex)
                {
                    throw new PaymentProviderException(
                        "Stripe payout reconciliation failed.",
                        ex,
                        ex.StripeError?.Code);
                }

                payoutDoc.Status = payout.Status ?? payoutDoc.Status;
                payoutDoc.FailureCode = payout.FailureCode;
                payoutDoc.FailureMessage = payout.FailureMessage;
                payoutDoc.ReserveStatus = MapReserveStatus(payoutDoc.Status);
                payoutDoc.UpdatedAt = Timestamp.GetCurrentTimestamp();

                await _firestore.Payouts.Document(payoutId).SetAsync(payoutDoc, SetOptions.Overwrite);

                var reserveRef = _firestore.PayoutReserves.Document(payoutDoc.ReserveId);
                var reserveSnap = await reserveRef.GetSnapshotAsync();
                if (reserveSnap.Exists)
                {
                    var reserveDoc = reserveSnap.ConvertTo<PayoutReserveDocument>();
                    reserveDoc.Status = payoutDoc.ReserveStatus;
                    reserveDoc.Reason = payoutDoc.Status;
                    reserveDoc.UpdatedAt = Timestamp.GetCurrentTimestamp();
                    await reserveRef.SetAsync(reserveDoc, SetOptions.Overwrite);
                }
            }

            return ToDto(payoutDoc);
        }

        private static string NormalizeCurrency(string? currency)
        {
            return string.IsNullOrWhiteSpace(currency)
                ? "usd"
                : currency.Trim().ToLowerInvariant();
        }

        private static string MapReserveStatus(string? payoutStatus)
        {
            if (string.IsNullOrWhiteSpace(payoutStatus))
                return "reserved";

            return payoutStatus.ToLowerInvariant() switch
            {
                "paid" => "consumed",
                "failed" => "released",
                "canceled" => "released",
                _ => "reserved"
            };
        }

        private static PayoutDto ToDto(PayoutDocument document)
        {
            return new PayoutDto
            {
                Id = document.Id,
                UserId = document.UserId,
                Amount = AmountConverter.FromMinorUnits(document.AmountMinorUnits),
                Currency = document.Currency,
                Status = document.Status,
                StripePayoutId = document.StripePayoutId,
                StripeAccountId = document.StripeAccountId,
                ReserveId = document.ReserveId,
                ReserveStatus = document.ReserveStatus,
                FailureCode = document.FailureCode,
                FailureMessage = document.FailureMessage,
                ExternalReference = document.ExternalReference,
                CreatedAt = document.CreatedAt.ToDateTime(),
                UpdatedAt = document.UpdatedAt.ToDateTime()
            };
        }

        private void EnsureStripeConfigured()
        {
            if (_stripePayoutService is null)
                throw new PaymentConfigurationException("Stripe:ApiKey is not configured.");
        }

        private sealed class PayoutServiceStripeAdapter
        {
            private readonly Stripe.PayoutService _inner;

            public PayoutServiceStripeAdapter(StripeClient client)
            {
                _inner = new Stripe.PayoutService(client);
            }

            public Task<Payout> CreateAsync(PayoutCreateOptions options, RequestOptions requestOptions)
            {
                return _inner.CreateAsync(options, requestOptions);
            }

            public Task<Payout> GetAsync(string payoutId, RequestOptions requestOptions)
            {
                return _inner.GetAsync(payoutId, null, requestOptions);
            }
        }
    }
}
