using System.Security.Claims;
using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;

namespace FinTechAPI.Tests.Controllers
{
    public class PayoutsControllerTests
    {
        private readonly Mock<IPayoutService> _mockService;
        private readonly PayoutsController _controller;

        private const string UserId = "firebase-user-1";

        public PayoutsControllerTests()
        {
            _mockService = new Mock<IPayoutService>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
            var mockAudit = new Mock<IAuditService>();
            _controller = new PayoutsController(_mockService.Object, mockEnvironment.Object, mockAudit.Object);

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim(ClaimTypes.Email, "user@example.com")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
        }

        // ── Happy paths ────────────────────────────────────────────────────

        [Fact]
        public async Task CreatePayout_ShouldReturnOk_WhenRequestValid()
        {
            var dto = new CreatePayoutDto { Amount = 50m, Currency = "usd" };

            _mockService
                .Setup(s => s.CreatePayoutAsync(dto, UserId, "idem-ok"))
                .ReturnsAsync(new PayoutDto
                {
                    Id = "po-1",
                    UserId = UserId,
                    Amount = 50m,
                    Currency = "usd",
                    Status = "pending"
                });

            var result = await _controller.CreatePayout(dto, "idem-ok");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<PayoutDto>(ok.Value);
            Assert.Equal("po-1", payload.Id);
        }

        [Fact]
        public async Task CreatePayout_ShouldGenerateFallbackKey_WhenHeaderMissing_InDevelopment()
        {
            var dto = new CreatePayoutDto { Amount = 10m, Currency = "usd" };

            _mockService
                .Setup(s => s.CreatePayoutAsync(dto, UserId, It.Is<string>(v => !string.IsNullOrWhiteSpace(v))))
                .ReturnsAsync(new PayoutDto
                {
                    Id = "po-fallback",
                    Amount = 10m,
                    Currency = "usd",
                    Status = "pending"
                });

            var result = await _controller.CreatePayout(dto, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<PayoutDto>(ok.Value);
            Assert.True(_controller.Response.Headers.ContainsKey("X-Idempotency-Key"));
        }

        [Fact]
        public async Task GetPayouts_ShouldReturnOk_WithList()
        {
            _mockService
                .Setup(s => s.GetPayoutsByUserIdAsync(UserId))
                .ReturnsAsync(new[] { new PayoutDto { Id = "po-1" }, new PayoutDto { Id = "po-2" } });

            var result = await _controller.GetPayouts();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2, ((IEnumerable<PayoutDto>)ok.Value!).Count());
        }

        [Fact]
        public async Task GetPayout_ShouldReturnOk_WhenFound()
        {
            var dto = new PayoutDto { Id = "po-1", UserId = UserId, Amount = 25m, Status = "paid" };

            _mockService.Setup(s => s.GetPayoutByIdAsync("po-1", UserId)).ReturnsAsync(dto);

            var result = await _controller.GetPayout("po-1");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task ReconcilePayout_ShouldReturnOk_WhenFound()
        {
            var dto = new PayoutDto { Id = "po-1", UserId = UserId, Amount = 25m, Status = "paid" };

            _mockService.Setup(s => s.ReconcilePayoutAsync("po-1", UserId)).ReturnsAsync(dto);

            var result = await _controller.ReconcilePayout("po-1");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dto, ok.Value);
        }

        // ── Negative scenarios ─────────────────────────────────────────────

        [Fact]
        public async Task CreatePayout_ShouldReturnBadRequest_WhenHeaderMissing_InProduction()
        {
            var dto = new CreatePayoutDto { Amount = 10m, Currency = "usd" };

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            var controller = new PayoutsController(_mockService.Object, environment.Object, new Mock<IAuditService>().Object)
            {
                ControllerContext = _controller.ControllerContext
            };

            var result = await controller.CreatePayout(dto, null);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePayout_ShouldReturnBadRequest_WhenArgumentException()
        {
            var dto = new CreatePayoutDto { Amount = 10m, Currency = "usd" };

            _mockService
                .Setup(s => s.CreatePayoutAsync(dto, UserId, "idem-bad"))
                .ThrowsAsync(new ArgumentException("Invalid payout parameters."));

            var result = await _controller.CreatePayout(dto, "idem-bad");

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePayout_ShouldReturn503_WhenStripeNotConfigured()
        {
            var dto = new CreatePayoutDto { Amount = 10m, Currency = "usd" };

            _mockService
                .Setup(s => s.CreatePayoutAsync(dto, UserId, "idem-503"))
                .ThrowsAsync(new PaymentConfigurationException("Stripe:ApiKey is not configured."));

            var result = await _controller.CreatePayout(dto, "idem-503");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, status.StatusCode);
        }

        [Fact]
        public async Task CreatePayout_ShouldReturn502_WhenStripeRejectsRequest()
        {
            var dto = new CreatePayoutDto { Amount = 10m, Currency = "usd" };

            _mockService
                .Setup(s => s.CreatePayoutAsync(dto, UserId, "idem-502"))
                .ThrowsAsync(new PaymentProviderException("Insufficient funds.", "insufficient_funds"));

            var result = await _controller.CreatePayout(dto, "idem-502");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(502, status.StatusCode);
        }

        [Fact]
        public async Task GetPayout_ShouldReturnNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.GetPayoutByIdAsync("po-x", UserId)).ReturnsAsync((PayoutDto?)null);

            var result = await _controller.GetPayout("po-x");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ReconcilePayout_ShouldReturnNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.ReconcilePayoutAsync("po-x", UserId)).ReturnsAsync((PayoutDto?)null);

            var result = await _controller.ReconcilePayout("po-x");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ReconcilePayout_ShouldReturn502_WhenProviderFails()
        {
            _mockService
                .Setup(s => s.ReconcilePayoutAsync("po-1", UserId))
                .ThrowsAsync(new PaymentProviderException("Stripe error.", "api_error"));

            var result = await _controller.ReconcilePayout("po-1");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(502, status.StatusCode);
        }

        [Fact]
        public async Task ReconcilePayout_ShouldReturn503_WhenConfigurationMissing()
        {
            _mockService
                .Setup(s => s.ReconcilePayoutAsync("po-1", UserId))
                .ThrowsAsync(new PaymentConfigurationException("Stripe:ApiKey is not configured."));

            var result = await _controller.ReconcilePayout("po-1");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, status.StatusCode);
        }
    }
}
