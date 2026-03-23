using AutoMapper;
using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinTechAPI.Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private readonly Mock<ITransactionService> _mockService;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly TransactionsController _controller;

        private const string UserId = "firebase-user-1";

        public TransactionsControllerTests()
        {
            _mockService = new Mock<ITransactionService>();
            _mockPaymentService = new Mock<IPaymentService>();
            _mockMapper = new Mock<IMapper>();

            _controller = new TransactionsController(_mockService.Object, _mockPaymentService.Object, _mockMapper.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = ControllerTestHelpers.CreateHttpContext(UserId, "user@example.com")
            };
        }

        [Fact]
        public async Task GetTransactions_ShouldReturnOkWithList()
        {
            var transactions = new List<Transaction> { new() { Id = "tx-1", Amount = 100 } };
            var dtos = new List<TransactionDto> { new() { Id = "tx-1", Amount = 100 } };

            _mockService.Setup(s => s.GetTransactionsAsync(UserId)).ReturnsAsync(transactions);
            _mockMapper.Setup(m => m.Map<IEnumerable<TransactionDto>>(transactions)).Returns(dtos);
            _mockPaymentService.Setup(s => s.GetPaymentsByUserIdAsync(UserId)).ReturnsAsync(Array.Empty<PaymentDto>());

            var result = await _controller.GetTransactions();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Single((IEnumerable<TransactionDto>)ok.Value!);
        }

        [Fact]
        public async Task GetTransaction_ShouldReturnOk_WhenFound()
        {
            var txn = new Transaction { Id = "tx-1", Amount = 50 };
            var dto = new TransactionDto { Id = "tx-1", Amount = 50 };

            _mockService.Setup(s => s.GetTransactionByIdAsync("tx-1", UserId)).ReturnsAsync(txn);
            _mockMapper.Setup(m => m.Map<TransactionDto>(txn)).Returns(dto);
            _mockPaymentService.Setup(s => s.GetPaymentsByUserIdAsync(UserId)).ReturnsAsync(Array.Empty<PaymentDto>());

            var result = await _controller.GetTransaction("tx-1");

            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTransaction_ShouldReturnNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.GetTransactionByIdAsync("x", UserId)).ReturnsAsync((Transaction)null!);

            var result = await _controller.GetTransaction("x");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateTransaction_ShouldReturnCreatedAtAction()
        {
            var dto = new CreateTransactionDto { Amount = 100, Currency = Currency.USD, Type = TransactionType.Income, AccountId = "acc-1", TransactionDate = DateTime.UtcNow, Category = "Salary" };
            var created = new Transaction { Id = "tx-new", Amount = 100, UserId = UserId };
            var resultDto = new TransactionDto { Id = "tx-new", Amount = 100 };

            _mockService.Setup(s => s.CreateTransactionAsync(It.IsAny<Transaction>(), UserId)).ReturnsAsync(created);
            _mockMapper.Setup(m => m.Map<TransactionDto>(created)).Returns(resultDto);
            _mockPaymentService.Setup(s => s.GetPaymentsByUserIdAsync(UserId)).ReturnsAsync(Array.Empty<PaymentDto>());

            var result = await _controller.CreateTransaction(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("tx-new", createdResult.RouteValues!["id"]);
        }

        [Fact]
        public async Task CreateTransaction_ShouldReturnBadRequest_WhenAccountInvalid()
        {
            var dto = new CreateTransactionDto { Amount = 100, AccountId = "bad-acc", Type = TransactionType.Expense, Currency = Currency.USD, TransactionDate = DateTime.UtcNow, Category = "Test" };
            _mockService.Setup(s => s.CreateTransactionAsync(It.IsAny<Transaction>(), UserId)).ReturnsAsync((Transaction)null!);

            var result = await _controller.CreateTransaction(dto);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteTransaction_ShouldReturnNoContent_WhenSuccessful()
        {
            _mockService.Setup(s => s.DeleteTransactionAsync("tx-1", UserId)).ReturnsAsync(true);

            var result = await _controller.DeleteTransaction("tx-1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteTransaction_ShouldReturnNotFound_WhenMissing()
        {
            _mockService.Setup(s => s.DeleteTransactionAsync("x", UserId)).ReturnsAsync(false);

            var result = await _controller.DeleteTransaction("x");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateTransactionStatus_ShouldReturnOk_WhenUpdated()
        {
            var updated = new Transaction
            {
                Id = "tx-1",
                Amount = 100,
                Status = TransactionStatus.Failed,
                Category = "Payment"
            };
            var dto = new TransactionDto
            {
                Id = "tx-1",
                Amount = 100,
                Status = TransactionStatus.Failed,
                Category = "Payment"
            };

            _mockService
                .Setup(s => s.UpdateTransactionStatusAsync("tx-1", TransactionStatus.Failed, UserId))
                .ReturnsAsync(updated);
            _mockMapper.Setup(m => m.Map<TransactionDto>(updated)).Returns(dto);
            _mockPaymentService.Setup(s => s.GetPaymentsByUserIdAsync(UserId)).ReturnsAsync(Array.Empty<PaymentDto>());

            var result = await _controller.UpdateTransactionStatus(
                "tx-1",
                new UpdateTransactionStatusDto { Status = TransactionStatus.Failed });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("tx-1", ((TransactionDto)ok.Value!).Id);
        }

        [Fact]
        public async Task GetTransaction_ShouldPopulateProviderStatus_WhenLinkedPaymentExists()
        {
            var txn = new Transaction { Id = "tx-1", Amount = 50 };
            var dto = new TransactionDto
            {
                Id = "tx-1",
                Amount = 50,
                Status = TransactionStatus.Succeeded,
                BusinessStatus = TransactionStatus.Succeeded
            };

            _mockService.Setup(s => s.GetTransactionByIdAsync("tx-1", UserId)).ReturnsAsync(txn);
            _mockMapper.Setup(m => m.Map<TransactionDto>(txn)).Returns(dto);
            _mockPaymentService
                .Setup(s => s.GetPaymentsByUserIdAsync(UserId))
                .ReturnsAsync(new[]
                {
                    new PaymentDto
                    {
                        Id = "pay-1",
                        TransactionId = "tx-1",
                        Status = "succeeded",
                        StripePaymentIntentId = "pi_123",
                        LastWebhookEvent = "payment_intent.succeeded",
                        LastStripeEventId = "evt_123",
                        UpdatedAt = DateTime.UtcNow
                    }
                });

            var result = await _controller.GetTransaction("tx-1");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<TransactionDto>(ok.Value);
            Assert.Equal(TransactionStatus.Succeeded, response.BusinessStatus);
            Assert.Equal("succeeded", response.ProviderStatus);
            Assert.Equal("pi_123", response.ProviderReference);
            Assert.Equal("pay-1", response.PaymentId);
            Assert.Equal("payment_intent.succeeded", response.WebhookEvent);
            Assert.Equal("evt_123", response.CorrelationId);
        }

        // ── Negative scenarios ─────────────────────────────────────────────

        [Fact]
        public async Task UpdateTransaction_ShouldReturnNotFound_WhenMissing()
        {
            var dto = new CreateTransactionDto
            {
                Amount = 100,
                AccountId = "acc-1",
                Type = TransactionType.Expense,
                Currency = Currency.USD,
                TransactionDate = DateTime.UtcNow,
                Category = "Test"
            };
            _mockService
                .Setup(s => s.UpdateTransactionAsync("tx-x", It.IsAny<Transaction>(), UserId))
                .ReturnsAsync((Transaction)null!);

            var result = await _controller.UpdateTransaction("tx-x", dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateTransactionStatus_ShouldReturnNotFound_WhenMissing()
        {
            _mockService
                .Setup(s => s.UpdateTransactionStatusAsync("tx-x", TransactionStatus.Failed, UserId))
                .ReturnsAsync((Transaction)null!);

            var result = await _controller.UpdateTransactionStatus(
                "tx-x",
                new UpdateTransactionStatusDto { Status = TransactionStatus.Failed });

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetByAccount_ShouldReturnOk_WithEmptyList()
        {
            _mockService
                .Setup(s => s.GetTransactionsByAccountIdAsync("acc-empty", UserId))
                .ReturnsAsync(Array.Empty<Transaction>());
            _mockMapper
                .Setup(m => m.Map<IEnumerable<TransactionDto>>(It.IsAny<IEnumerable<Transaction>>()))
                .Returns(Array.Empty<TransactionDto>());
            _mockPaymentService.Setup(s => s.GetPaymentsByUserIdAsync(UserId)).ReturnsAsync(Array.Empty<PaymentDto>());

            var result = await _controller.GetByAccount("acc-empty");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty((IEnumerable<TransactionDto>)ok.Value!);
        }
    }
}
