using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditService _audit;

        public AuthController(IAuthService authService, IAuditService audit)
        {
            _authService = authService;
            _audit = audit;
        }

        private string? GetCorrelationId() =>
            HttpContext.Items.TryGetValue("CorrelationId", out var val) ? val as string : null;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerDto)
        {
            var (success, error, userDto) = await _authService.RegisterAsync(registerDto);
            if (!success)
                return BadRequest(new { message = error });

            await _audit.LogAsync(userDto!.Id, "User.Registered", "User", userDto.Id,
                new { registerDto.Email }, GetCorrelationId());
            return Ok(userDto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var authResponse = await _authService.LoginAsync(loginDto);
            if (!authResponse.Success)
                return Unauthorized(new { message = authResponse.ErrorMessage ?? "Invalid credentials." });

            await _audit.LogAsync(authResponse.User?.Id ?? "unknown", "User.LoggedIn", "User", authResponse.User?.Id,
                new { loginDto.Email }, GetCorrelationId());

            // Store token in HttpOnly cookie for browser/MAUI clients
            Response.Cookies.Append("Authorization", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = authResponse.Expiration
            });

            return Ok(authResponse);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
        {
            var authResponse = await _authService.RefreshTokenAsync(dto.RefreshToken);
            if (!authResponse.Success)
                return Unauthorized(new { message = authResponse.ErrorMessage ?? "Token refresh failed." });

            Response.Cookies.Append("Authorization", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = authResponse.Expiration
            });

            return Ok(authResponse);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var result = await _authService.SendPasswordResetEmailAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [Authorize]
        [HttpPost("send-verification-email")]
        public async Task<IActionResult> SendVerificationEmail()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length)
                : string.Empty;

            var result = await _authService.SendEmailVerificationAsync(token);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto)
        {
            var result = await _authService.VerifyEmailAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
    }
}
