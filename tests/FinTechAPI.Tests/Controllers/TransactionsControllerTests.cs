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
        private readonly Mock<IMapper>             _mockMapper;
        private readonly TransactionsController    _controller;

        private const string UserId = "firebase-user-1";

        public TransactionsControllerTests()
        {
            _mockService = new Mock<ITransactionService>();
            _mockMapper  = new Mock<IMapper>();

            _controller = new TransactionsController(_mockService.Object, _mockMapper.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = ControllerTestHelpers.CreateHttpContext(UserId, "user@example.com")
            };
        }

        [Fact]
        public async Task GetTransactions_ShouldReturnOkWithList()
        {
            var transactions = new List<Transaction> { new() { Id = "tx-1", Amount = 100 } };
            var dtos         = new List<TransactionDto> { new() { Id = "tx-1", Amount = 100 } };

            _mockService.Setup(s => s.GetTransactionsAsync(UserId)).ReturnsAsync(transactions);
            _mockMapper.Setup(m => m.Map<IEnumerable<TransactionDto>>(transactions)).Returns(dtos);

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
            var dto     = new CreateTransactionDto { Amount = 100, Currency = Currency.USD, Type = TransactionType.Income, AccountId = "acc-1", TransactionDate = DateTime.UtcNow, Category = "Salary" };
            var created = new Transaction { Id = "tx-new", Amount = 100, UserId = UserId };
            var result_dto = new TransactionDto { Id = "tx-new", Amount = 100 };

            _mockService.Setup(s => s.CreateTransactionAsync(It.IsAny<Transaction>(), UserId)).ReturnsAsync(created);
            _mockMapper.Setup(m => m.Map<TransactionDto>(created)).Returns(result_dto);

            var result = await _controller.CreateTransaction(dto);

            var created_result = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("tx-new", created_result.RouteValues!["id"]);
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
    }
}
