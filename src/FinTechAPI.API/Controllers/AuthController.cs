using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinTechAPI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerDto)
        {
            var (success, error, userDto) = await _authService.RegisterAsync(registerDto);
            if (!success)
                return BadRequest(new { message = error });
            return Ok(userDto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var authResponse = await _authService.LoginAsync(loginDto);
            if (!authResponse.Success)
                return Unauthorized(new { message = authResponse.ErrorMessage ?? "Invalid credentials." });

            // Store token in HttpOnly cookie for browser/MAUI clients
            Response.Cookies.Append("Authorization", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.None,
                Expires  = authResponse.Expiration
            });

            return Ok(authResponse);
        }
    }
}
