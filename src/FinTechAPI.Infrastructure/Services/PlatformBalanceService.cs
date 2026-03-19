using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Payments;
using Microsoft.Extensions.Logging;
using Stripe;

namespace FinTechAPI.Infrastructure.Services
{
    public class PlatformBalanceService : IPlatformBalanceService
    {
        private readonly IStripeBalanceService _stripeBalanceService;
        private readonly ILogger<PlatformBalanceService> _logger;

        public PlatformBalanceService(IStripeBalanceService stripeBalanceService, ILogger<PlatformBalanceService> logger)
        {
            _stripeBalanceService = stripeBalanceService;
            _logger = logger;
        }

        public async Task<PlatformBalanceDto> GetPlatformBalanceAsync(string currency, CancellationToken cancellationToken = default)
        {
            var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
                ? "usd"
                : currency.Trim().ToLowerInvariant();

            try
            {
                var stripeBalance = await _stripeBalanceService.GetAsync(cancellationToken: cancellationToken);
                var availableMinor = SumByCurrency(stripeBalance.Available, normalizedCurrency);
                var pendingMinor = SumByCurrency(stripeBalance.Pending, normalizedCurrency);

                return new PlatformBalanceDto
                {
                    Available = ToMajorUnits(availableMinor),
                    Pending = ToMajorUnits(pendingMinor),
                    Currency = normalizedCurrency,
                    Source = "stripe",
                    SyncedAt = DateTime.UtcNow
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe balance fetch failed. currency={Currency} stripeCode={StripeCode}",
                    normalizedCurrency,
                    ex.StripeError?.Code);

                throw new PaymentProviderException(
                    "Stripe balance request failed.",
                    ex,
                    ex.StripeError?.Code);
            }
        }

        private static long SumByCurrency(IEnumerable<BalanceAmount>? amounts, string currency)
        {
            if (amounts is null)
                return 0;

            return amounts
                .Where(a => string.Equals(a.Currency, currency, StringComparison.OrdinalIgnoreCase))
                .Sum(a => a.Amount);
        }

        private static decimal ToMajorUnits(long minorUnits) => minorUnits / 100m;
    }
}