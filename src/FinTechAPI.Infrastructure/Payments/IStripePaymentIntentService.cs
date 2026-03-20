using Stripe;

namespace FinTechAPI.Infrastructure.Payments;

/// <summary>
/// Abstraction over Stripe's PaymentIntentService to allow unit-testing without real Stripe calls.
/// </summary>
public interface IStripePaymentIntentService
{
    Task<PaymentIntent> CreateAsync(
        PaymentIntentCreateOptions options,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default);

    Task<PaymentIntent> GetAsync(
        string id,
        RequestOptions? requestOptions = null,
        CancellationToken cancellationToken = default);
}
