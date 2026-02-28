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
            var (result, userDto) = await _authService.RegisterAsync(registerDto);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            return Ok(userDto);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var authResponse = await _authService.LoginAsync(loginDto, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (authResponse == null)
                return Unauthorized(new { message = "Invalid email or password" });

            Response.Cookies.Append("Authorization", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = authResponse.Expiration
            });

            return Ok(authResponse);
        }
    }
}
