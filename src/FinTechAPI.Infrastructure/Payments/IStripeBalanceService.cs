using Stripe;

namespace FinTechAPI.Infrastructure.Payments;

/// <summary>
/// Abstraction over Stripe's BalanceService to allow unit-testing without real Stripe calls.
/// </summary>
public interface IStripeBalanceService
{
    Task<Balance> GetAsync(BalanceGetOptions? options = null, RequestOptions? requestOptions = null, CancellationToken cancellationToken = default);
}