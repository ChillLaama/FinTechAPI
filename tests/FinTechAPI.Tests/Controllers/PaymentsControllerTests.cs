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
    public class PaymentsControllerTests
    {
        private readonly Mock<IPaymentService> _mockService;
        private readonly PaymentsController _controller;

        private const string UserId = "firebase-user-1";

        public PaymentsControllerTests()
        {
            _mockService = new Mock<IPaymentService>();
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
            var mockAudit = new Mock<IAuditService>();
            _controller = new PaymentsController(_mockService.Object, mockEnvironment.Object, mockAudit.Object);

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

        [Fact]
        public async Task CreatePaymentIntent_ShouldGenerateFallbackIdempotencyKey_WhenHeaderMissing_InDevelopment()
        {
            var dto = new CreatePaymentIntentDto
            {
                Amount = 10m,
                Currency = "usd"
            };

            _mockService
                .Setup(service => service.CreatePaymentIntentAsync(
                    dto,
                    UserId,
                    It.Is<string>(value => !string.IsNullOrWhiteSpace(value))))
                .ReturnsAsync(new PaymentIntentResponseDto
                {
                    PaymentId = "pay-fallback",
                    StripePaymentIntentId = "pi_fallback",
                    ClientSecret = "secret",
                    Status = "requires_payment_method",
                    Amount = 10m,
                    Currency = "usd"
                });

            var result = await _controller.CreatePaymentIntent(dto, null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<PaymentIntentResponseDto>(ok.Value);
            Assert.True(_controller.Response.Headers.ContainsKey("X-Idempotency-Key"));
        }

        [Fact]
        public async Task CreatePaymentIntent_ShouldReturnBadRequest_WhenHeaderMissing_InNonDevelopment()
        {
            var dto = new CreatePaymentIntentDto
            {
                Amount = 10m,
                Currency = "usd"
            };

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            var controller = new PaymentsController(_mockService.Object, environment.Object, new Mock<IAuditService>().Object)
            {
                ControllerContext = _controller.ControllerContext
            };

            var result = await controller.CreatePaymentIntent(dto, null);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePaymentIntent_ShouldReturnOk_WhenRequestValid()
        {
            var dto = new CreatePaymentIntentDto
            {
                Amount = 10m,
                Currency = "usd",
                Description = "Test payment"
            };

            _mockService
                .Setup(service => service.CreatePaymentIntentAsync(dto, UserId, "idem-123"))
                .ReturnsAsync(new PaymentIntentResponseDto
                {
                    PaymentId = "pay-1",
                    StripePaymentIntentId = "pi_123",
                    ClientSecret = "secret",
                    Status = "requires_payment_method",
                    Amount = 10m,
                    Currency = "usd"
                });

            var result = await _controller.CreatePaymentIntent(dto, "idem-123");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<PaymentIntentResponseDto>(ok.Value);
            Assert.Equal("pay-1", payload.PaymentId);
        }

        [Fact]
        public async Task GetPayment_ShouldReturnNotFound_WhenMissing()
        {
            _mockService
                .Setup(service => service.GetPaymentByIdAsync("pay-x", UserId))
                .ReturnsAsync((PaymentDto?)null);

            var result = await _controller.GetPayment("pay-x");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetPayment_ShouldReturnOk_WhenFound()
        {
            var dto = new PaymentDto
            {
                Id = "pay-1",
                UserId = UserId,
                Amount = 25.00m,
                Currency = "usd",
                Status = "succeeded",
                StripePaymentIntentId = "pi_abc",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _mockService
                .Setup(service => service.GetPaymentByIdAsync("pay-1", UserId))
                .ReturnsAsync(dto);

            var result = await _controller.GetPayment("pay-1");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task ReconcilePayment_ShouldReturnOk_WhenFound()
        {
            var dto = new PaymentDto
            {
                Id = "pay-1",
                UserId = UserId,
                Amount = 25.00m,
                Currency = "usd",
                Status = "processing",
                StripePaymentIntentId = "pi_abc",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _mockService
                .Setup(service => service.ReconcilePaymentAsync("pay-1", UserId))
                .ReturnsAsync(dto);

            var result = await _controller.ReconcilePayment("pay-1");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task ReconcilePayment_ShouldReturn502_WhenProviderFails()
        {
            _mockService
                .Setup(service => service.ReconcilePaymentAsync("pay-1", UserId))
                .ThrowsAsync(new PaymentProviderException("Stripe reconciliation request failed.", "api_error"));

            var result = await _controller.ReconcilePayment("pay-1");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(502, status.StatusCode);
        }

        // ── Exception-handling contracts ────────────────────────────────────

        [Fact]
        public async Task CreatePaymentIntent_Returns503_WhenStripeNotConfigured()
        {
            _mockService
                .Setup(s => s.CreatePaymentIntentAsync(
                    It.IsAny<CreatePaymentIntentDto>(), UserId, "idem-503"))
                .ThrowsAsync(new PaymentConfigurationException("Stripe:ApiKey is not configured."));

            var result = await _controller.CreatePaymentIntent(
                new CreatePaymentIntentDto { Amount = 10m, Currency = "usd" },
                "idem-503");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, status.StatusCode);
        }

        [Fact]
        public async Task CreatePaymentIntent_Returns502_WhenStripeRejectsRequest()
        {
            _mockService
                .Setup(s => s.CreatePaymentIntentAsync(
                    It.IsAny<CreatePaymentIntentDto>(), UserId, "idem-502"))
                .ThrowsAsync(new PaymentProviderException("Your card was declined.", "card_declined"));

            var result = await _controller.CreatePaymentIntent(
                new CreatePaymentIntentDto { Amount = 10m, Currency = "usd" },
                "idem-502");

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(502, status.StatusCode);
        }

        [Fact]
        public async Task StripeWebhook_Returns503_WhenWebhookSecretNotConfigured()
        {
            _mockService
                .Setup(s => s.HandleStripeWebhookAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new PaymentConfigurationException("Stripe:WebhookSecret is not configured."));

            var result = await _controller.StripeWebhook("t=1,v1=sig");

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, status.StatusCode);
        }
    }
}
