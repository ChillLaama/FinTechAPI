using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto, string userId, string idempotencyKey);
        Task<PaymentDto?> GetPaymentByIdAsync(string paymentId, string userId);
        Task<bool> HandleStripeWebhookAsync(string payload, string signatureHeader);
    }
}
