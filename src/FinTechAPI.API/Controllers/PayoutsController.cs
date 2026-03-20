using System.Security.Claims;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PayoutsController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        private readonly IWebHostEnvironment _environment;

        public PayoutsController(IPayoutService payoutService, IWebHostEnvironment environment)
        {
            _payoutService = payoutService;
            _environment = environment;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpPost]
        public async Task<ActionResult<PayoutDto>> CreatePayout(
            [FromBody] CreatePayoutDto dto,
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

                effectiveIdempotencyKey = Guid.NewGuid().ToString("N");
            }
            else
            {
                effectiveIdempotencyKey = idempotencyKey;
            }

            Response.Headers["X-Idempotency-Key"] = effectiveIdempotencyKey;

            try
            {
                var payout = await _payoutService.CreatePayoutAsync(dto, userId, effectiveIdempotencyKey);
                return Ok(payout);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayoutDto>>> GetPayouts()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var payouts = await _payoutService.GetPayoutsByUserIdAsync(userId);
            return Ok(payouts);
        }

        [HttpGet("{payoutId}")]
        public async Task<ActionResult<PayoutDto>> GetPayout(string payoutId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var payout = await _payoutService.GetPayoutByIdAsync(payoutId, userId);
            if (payout is null)
                return NotFound();

            return Ok(payout);
        }

        [HttpPost("{payoutId}/reconcile")]
        public async Task<ActionResult<PayoutDto>> ReconcilePayout(string payoutId)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var payout = await _payoutService.ReconcilePayoutAsync(payoutId, userId);
                if (payout is null)
                    return NotFound();

                return Ok(payout);
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
    }
}
