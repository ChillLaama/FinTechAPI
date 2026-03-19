using FinTechAPI.Application.Exceptions;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinTechAPI.Infrastructure.Payments;

/// <summary>
/// Thread-safe Stripe Balance service wrapper.
/// Uses a per-key StripeClient instance instead of global StripeConfiguration state.
/// </summary>
public sealed class StripeBalanceService : IStripeBalanceService
{
    private readonly BalanceService? _inner;
    private readonly bool _isConfigured;

    public StripeBalanceService(IOptions<StripeSettings> settings)
    {
        var apiKey = settings.Value.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var client = new StripeClient(apiKey);
            _inner = new BalanceService(client);
            _isConfigured = true;
        }
    }

    public Task<Balance> GetAsync(
        BalanceGetOptions? options = null,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _inner is null)
            throw new PaymentConfigurationException("Stripe:ApiKey is not configured.");

        return _inner.GetAsync(options, requestOptions, cancellationToken);
    }
}