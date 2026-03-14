using System.Security.Claims;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [Authorize]
        [HttpPost("intents")]
        public async Task<ActionResult<PaymentIntentResponseDto>> CreatePaymentIntent([FromBody] CreatePaymentIntentDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) ||
                string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return BadRequest(new { message = "Idempotency-Key header is required." });
            }

            var result = await _paymentService.CreatePaymentIntentAsync(dto, userId, idempotencyKey.ToString());
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{paymentId}")]
        public async Task<ActionResult<PaymentDto>> GetPayment(string paymentId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var payment = await _paymentService.GetPaymentByIdAsync(paymentId, userId);
            if (payment == null)
                return NotFound();

            return Ok(payment);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureHeader) ||
                string.IsNullOrWhiteSpace(signatureHeader))
            {
                return BadRequest(new { message = "Stripe-Signature header is required." });
            }

            var handled = await _paymentService.HandleStripeWebhookAsync(payload, signatureHeader.ToString());
            if (!handled)
                return BadRequest(new { message = "Invalid Stripe webhook signature." });

            return Ok();
        }
    }
}
