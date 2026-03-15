namespace FinTechAPI.Application.Exceptions;

/// <summary>Thrown when the Stripe API returns an error during payment processing.</summary>
public sealed class PaymentProviderException : Exception
{
    /// <summary>Stripe error code, if available.</summary>
    public string? StripeCode { get; }

    public PaymentProviderException(string message, string? stripeCode = null)
        : base(message)
    {
        StripeCode = stripeCode;
    }

    public PaymentProviderException(string message, Exception inner, string? stripeCode = null)
        : base(message, inner)
    {
        StripeCode = stripeCode;
    }
}
