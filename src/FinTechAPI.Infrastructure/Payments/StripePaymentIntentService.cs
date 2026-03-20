using FinTechAPI.Application.Exceptions;
using Microsoft.Extensions.Options;
using Stripe;

namespace FinTechAPI.Infrastructure.Payments;

/// <summary>
/// Thread-safe Stripe PaymentIntent service wrapper.
/// Uses a per-key StripeClient instance instead of the global StripeConfiguration.ApiKey,
/// which is not safe for concurrent use.
/// </summary>
public sealed class StripePaymentIntentService : IStripePaymentIntentService
{
    private readonly PaymentIntentService? _inner;
    private readonly bool _isConfigured;

    public StripePaymentIntentService(IOptions<StripeSettings> settings)
    {
        var apiKey = settings.Value.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var client = new StripeClient(apiKey);
            _inner = new PaymentIntentService(client);
            _isConfigured = true;
        }
    }

    public Task<PaymentIntent> CreateAsync(
        PaymentIntentCreateOptions options,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _inner is null)
            throw new PaymentConfigurationException("Stripe:ApiKey is not configured.");

        return _inner.CreateAsync(options, requestOptions, cancellationToken);
    }

    public Task<PaymentIntent> GetAsync(
        string id,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured || _inner is null)
            throw new PaymentConfigurationException("Stripe:ApiKey is not configured.");

        return _inner.GetAsync(id, null, requestOptions, cancellationToken);
    }
}
