namespace FinTechAPI.Application.Exceptions;

/// <summary>Thrown when a required Stripe configuration value (ApiKey, WebhookSecret) is missing.</summary>
public sealed class PaymentConfigurationException : Exception
{
    public PaymentConfigurationException(string message) : base(message) { }
}
