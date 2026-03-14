using System.Security.Claims;
using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            _controller = new PaymentsController(_mockService.Object);

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
        public async Task CreatePaymentIntent_ShouldReturnBadRequest_WhenIdempotencyHeaderMissing()
        {
            var dto = new CreatePaymentIntentDto
            {
                Amount = 10m,
                Currency = "usd"
            };

            var result = await _controller.CreatePaymentIntent(dto);

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

            _controller.HttpContext.Request.Headers["Idempotency-Key"] = "idem-123";
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

            var result = await _controller.CreatePaymentIntent(dto);

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
    }
}
