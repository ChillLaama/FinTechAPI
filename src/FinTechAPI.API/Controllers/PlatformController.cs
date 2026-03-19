using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/platform")]
    public class PlatformController : ControllerBase
    {
        private readonly IPlatformBalanceService _platformBalanceService;

        public PlatformController(IPlatformBalanceService platformBalanceService)
        {
            _platformBalanceService = platformBalanceService;
        }

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
    }
}