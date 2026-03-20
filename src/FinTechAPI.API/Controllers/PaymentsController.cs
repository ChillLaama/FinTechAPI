using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
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
        private readonly IWebHostEnvironment _environment;

        public PaymentsController(IPaymentService paymentService, IWebHostEnvironment environment)
        {
            _paymentService = paymentService;
            _environment = environment;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [Authorize]
        [HttpPost("intents")]
        public async Task<ActionResult<PaymentIntentResponseDto>> CreatePaymentIntent(
            [FromBody] CreatePaymentIntentDto dto,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            string effectiveIdempotencyKey;
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                if (!_environment.IsDevelopment())
                    return BadRequest(new { message = "Idempotency-Key header is required." });

                // Fallback for Swagger/dev only: generate key server-side.
                effectiveIdempotencyKey = Guid.NewGuid().ToString("N");
            }
            else
            {
                effectiveIdempotencyKey = idempotencyKey;
            }

            Response.Headers["X-Idempotency-Key"] = effectiveIdempotencyKey;

            try
            {
                var result = await _paymentService.CreatePaymentIntentAsync(dto, userId, effectiveIdempotencyKey);
                return Ok(result);
            }
            catch (PaymentConfigurationException ex)
            {
                return StatusCode(503, new { message = ex.Message });
            }
            catch (PaymentProviderException ex)
            {
                return StatusCode(502, new { message = ex.Message, stripeCode = ex.StripeCode });
            }
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

        [Authorize]
        [HttpPost("{paymentId}/reconcile")]
        public async Task<ActionResult<PaymentDto>> ReconcilePayment(string paymentId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var payment = await _paymentService.ReconcilePaymentAsync(paymentId, userId);
                if (payment == null)
                    return NotFound();

                return Ok(payment);
            }
            catch (PaymentConfigurationException ex)
            {
                return StatusCode(503, new { message = ex.Message });
            }
            catch (PaymentProviderException ex)
            {
                return StatusCode(502, new { message = ex.Message, stripeCode = ex.StripeCode });
            }
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook(
            [FromHeader(Name = "Stripe-Signature")][Required] string signatureHeader)
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                return BadRequest(new { message = "Stripe-Signature header is required." });
            }

            try
            {
                var handled = await _paymentService.HandleStripeWebhookAsync(payload, signatureHeader);
                if (!handled)
                    return BadRequest(new { message = "Invalid Stripe webhook signature." });
            }
            catch (PaymentConfigurationException ex)
            {
                return StatusCode(503, new { message = ex.Message });
            }

            return Ok();
        }
    }
}
