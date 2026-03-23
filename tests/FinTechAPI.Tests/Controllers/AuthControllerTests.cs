using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinTechAPI.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            var mockAudit = new Mock<IAuditService>();
            _controller = new AuthController(_mockAuthService.Object, mockAudit.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = ControllerTestHelpers.CreateHttpContext("uid", "test@example.com")
            };
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenSuccessful()
        {
            var dto = new RegisterUserDto { Email = "new@test.com", Password = "Pass123!", FirstName = "A", LastName = "B" };
            var userDto = new UserDto { Id = "uid-1", Email = "new@test.com", FirstName = "A", LastName = "B" };

            _mockAuthService.Setup(s => s.RegisterAsync(dto))
                .ReturnsAsync((true, (string?)null, userDto));

            var result = await _controller.Register(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(userDto, ok.Value);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenFailed()
        {
            var dto = new RegisterUserDto { Email = "bad@test.com", Password = "weak" };
            _mockAuthService.Setup(s => s.RegisterAsync(dto))
                .ReturnsAsync((false, "Email already in use.", (UserDto?)null));

            var result = await _controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
        {
            var dto = new LoginDto { Email = "test@test.com", Password = "Pass123!" };
            var response = new AuthResponseDto
            {
                Success = true,
                Token = "firebase-id-token",
                RefreshToken = "refresh-token",
                Expiration = DateTime.UtcNow.AddHours(1),
                User = new UserDto { Id = "uid-1", Email = "test@test.com" }
            };

            _mockAuthService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(response);

            var result = await _controller.Login(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var res = Assert.IsType<AuthResponseDto>(ok.Value);
            Assert.True(res.Success);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsInvalid()
        {
            var dto = new LoginDto { Email = "wrong@test.com", Password = "wrong" };
            _mockAuthService.Setup(s => s.LoginAsync(dto))
                .ReturnsAsync(new AuthResponseDto { Success = false, ErrorMessage = "Invalid credentials." });

            var result = await _controller.Login(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── Refresh token scenarios ────────────────────────────────────────

        [Fact]
        public async Task RefreshToken_ShouldReturnOk_WhenValid()
        {
            var dto = new RefreshTokenDto { RefreshToken = "valid-refresh" };
            var response = new AuthResponseDto
            {
                Success = true,
                Token = "new-id-token",
                RefreshToken = "new-refresh-token",
                Expiration = DateTime.UtcNow.AddHours(1),
                User = new UserDto { Id = "uid-1", Email = "test@test.com" }
            };

            _mockAuthService.Setup(s => s.RefreshTokenAsync("valid-refresh")).ReturnsAsync(response);

            var result = await _controller.RefreshToken(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var res = Assert.IsType<AuthResponseDto>(ok.Value);
            Assert.True(res.Success);
        }

        [Fact]
        public async Task RefreshToken_ShouldReturnUnauthorized_WhenInvalid()
        {
            var dto = new RefreshTokenDto { RefreshToken = "expired-token" };
            _mockAuthService.Setup(s => s.RefreshTokenAsync("expired-token"))
                .ReturnsAsync(new AuthResponseDto { Success = false, ErrorMessage = "Token has expired." });

            var result = await _controller.RefreshToken(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── Password reset negative scenarios ──────────────────────────────

        [Fact]
        public async Task ForgotPassword_ShouldReturnBadRequest_WhenFailed()
        {
            var dto = new ForgotPasswordDto { Email = "noexist@test.com" };
            _mockAuthService.Setup(s => s.SendPasswordResetEmailAsync(dto))
                .ReturnsAsync(new AuthOperationResultDto { Success = false, Message = "Email not found." });

            var result = await _controller.ForgotPassword(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ShouldReturnBadRequest_WhenFailed()
        {
            var dto = new ResetPasswordDto { OobCode = "bad-code", NewPassword = "P@ss1!" };
            _mockAuthService.Setup(s => s.ResetPasswordAsync(dto))
                .ReturnsAsync(new AuthOperationResultDto { Success = false, Message = "Invalid or expired code." });

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── Register exception propagation ─────────────────────────────────

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenServiceReportsWeakPassword()
        {
            var dto = new RegisterUserDto { Email = "a@b.com", Password = "123", FirstName = "A", LastName = "B" };
            _mockAuthService.Setup(s => s.RegisterAsync(dto))
                .ReturnsAsync((false, "WEAK_PASSWORD : Password should be at least 6 characters", (UserDto?)null));

            var result = await _controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
