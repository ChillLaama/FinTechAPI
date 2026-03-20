using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinTechAPI.Tests.Controllers
{
    public class PlatformControllerTests
    {
        private readonly Mock<IPlatformBalanceService> _mockBalanceService;
        private readonly Mock<IPlatformSummaryService> _mockSummaryService;
        private readonly PlatformController _controller;

        private const string UserId = "firebase-user-1";

        public PlatformControllerTests()
        {
            _mockBalanceService = new Mock<IPlatformBalanceService>();
            _mockSummaryService = new Mock<IPlatformSummaryService>();

            _controller = new PlatformController(_mockBalanceService.Object, _mockSummaryService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = ControllerTestHelpers.CreateHttpContext(UserId, "user@example.com")
                }
            };
        }

        [Fact]
        public async Task GetBalance_ShouldReturnOk_WhenAvailable()
        {
            var dto = new PlatformBalanceDto
            {
                Available = 12.34m,
                Pending = 5.00m,
                Currency = "usd",
                Source = "stripe",
                SyncedAt = DateTime.UtcNow
            };

            _mockBalanceService
                .Setup(s => s.GetPlatformBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            var result = await _controller.GetBalance();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task GetBalance_ShouldReturn503_WhenConfigurationMissing()
        {
            _mockBalanceService
                .Setup(s => s.GetPlatformBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new PaymentConfigurationException("Stripe:ApiKey is not configured."));

            var result = await _controller.GetBalance();

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetSummary_ShouldReturnOk_WhenAvailable()
        {
            var dto = new PlatformSummaryDto
            {
                ProcessedVolume = 100m,
                SuccessfulPayments = 3,
                FailedPayments = 1,
                PendingReviewCount = 0,
                FraudBlockedCount = 0,
                Currency = "usd",
                Source = "fintechapi+stripe",
                SyncedAt = DateTime.UtcNow
            };

            _mockSummaryService
                .Setup(s => s.GetPlatformSummaryAsync(UserId, "usd", It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            var result = await _controller.GetSummary();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(dto, ok.Value);
        }
    }
}
