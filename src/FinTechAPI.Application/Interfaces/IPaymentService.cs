using FinTechAPI.Application.DTOs;

namespace FinTechAPI.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto, string userId, string idempotencyKey);
        Task<PaymentDto?> GetPaymentByIdAsync(string paymentId, string userId);
        Task<IEnumerable<PaymentDto>> GetPaymentsByUserIdAsync(string userId);
        Task<PaymentDto?> ReconcilePaymentAsync(string paymentId, string userId);
        Task<bool> HandleStripeWebhookAsync(string payload, string signatureHeader);

        /// <summary>Returns payments that are in a non-terminal, potentially stuck state (admin-only view).</summary>
        Task<IReadOnlyList<PendingPaymentDto>> GetPendingPaymentsForAdminAsync(int staleAfterMinutes = 5, int limit = 100);
    }
}


