using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/platform")]
    public class PlatformController : ControllerBase
    {
        private readonly IPlatformBalanceService _platformBalanceService;
        private readonly IPlatformSummaryService _platformSummaryService;

        public PlatformController(
            IPlatformBalanceService platformBalanceService,
            IPlatformSummaryService platformSummaryService)
        {
            _platformBalanceService = platformBalanceService;
            _platformSummaryService = platformSummaryService;
        }

        private string GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [Authorize]
        [HttpGet("balance")]
        public async Task<ActionResult<PlatformBalanceDto>> GetBalance(
            [FromQuery] string currency = "usd",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var balance = await _platformBalanceService.GetPlatformBalanceAsync(currency, cancellationToken);
                return Ok(balance);
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
        [HttpGet("summary")]
        public async Task<ActionResult<PlatformSummaryDto>> GetSummary(
            [FromQuery] string currency = "usd",
            CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var summary = await _platformSummaryService.GetPlatformSummaryAsync(userId, currency, cancellationToken);
            return Ok(summary);
        }
    }
}