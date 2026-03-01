using AutoMapper;
using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinTechAPI.Tests.Controllers
{
    public class AccountsControllerTests
    {
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly Mock<IMapper>         _mockMapper;
        private readonly AccountsController    _controller;

        private const string TestUserId    = "firebase-uid-test";
        private const string TestUserEmail = "test@example.com";

        public AccountsControllerTests()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockMapper         = new Mock<IMapper>();

            _controller = new AccountsController(_mockAccountService.Object, _mockMapper.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = ControllerTestHelpers.CreateHttpContext(TestUserId, TestUserEmail)
            };
        }

        [Fact]
        public async Task GetAccounts_ShouldReturnOkWithAccountDtos()
        {
            var dtos = new List<AccountDto>
            {
                new() { Id = "acc-1", Name = "Checking", Balance = 100 },
                new() { Id = "acc-2", Name = "Savings",  Balance = 500 }
            };
            _mockAccountService.Setup(s => s.GetAccountsByUserIdAsync(TestUserId)).ReturnsAsync(dtos);

            var result = await _controller.GetAccounts();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(2, ((IEnumerable<AccountDto>)ok.Value!).Count());
        }

        [Fact]
        public async Task GetAccount_ShouldReturnOk_WhenAccountExists()
        {
            const string accountId = "acc-123";
            var account    = new Account { Id = accountId, Name = "Test", UserId = TestUserId };
            var accountDto = new AccountDto { Id = accountId, Name = "Test", Balance = 0 };

            _mockAccountService.Setup(s => s.GetAccountByIdAsync(accountId, TestUserId)).ReturnsAsync(account);
            _mockMapper.Setup(m => m.Map<AccountDto>(account)).Returns(accountDto);

            var result = await _controller.GetAccount(accountId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(accountId, ((AccountDto)ok.Value!).Id);
        }

        [Fact]
        public async Task GetAccount_ShouldReturnNotFound_WhenMissing()
        {
            _mockAccountService.Setup(s => s.GetAccountByIdAsync("missing", TestUserId)).ReturnsAsync((Account)null!);

            var result = await _controller.GetAccount("missing");

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateAccount_ShouldReturnCreatedAtAction()
        {
            var newAccount     = new Account { Name = "New", AccountType = AccountType.Savings, Currency = Currency.EUR };
            var createdAccount = new Account { Id = "new-id", Name = "New", UserId = TestUserId };

            _mockAccountService.Setup(s => s.CreateAccountAsync(It.IsAny<Account>(), TestUserId)).ReturnsAsync(createdAccount);

            var result = await _controller.CreateAccount(newAccount);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("new-id", created.RouteValues!["id"]);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnNoContent_WhenSuccessful()
        {
            const string accountId    = "acc-1";
            var updated               = new Account { Id = accountId, Name = "Updated", UserId = TestUserId };
            _mockAccountService.Setup(s => s.UpdateAccountAsync(accountId, It.IsAny<Account>(), TestUserId)).ReturnsAsync(updated);

            var result = await _controller.UpdateAccount(accountId, new Account());

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnNotFound_WhenMissing()
        {
            _mockAccountService.Setup(s => s.UpdateAccountAsync("x", It.IsAny<Account>(), TestUserId)).ReturnsAsync((Account)null!);

            var result = await _controller.UpdateAccount("x", new Account());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAccount_ShouldReturnNoContent_WhenSuccessful()
        {
            _mockAccountService.Setup(s => s.DeleteAccountAsync("del-1", TestUserId)).ReturnsAsync(true);

            var result = await _controller.DeleteAccount("del-1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAccount_ShouldReturnNotFound_WhenMissing()
        {
            _mockAccountService.Setup(s => s.DeleteAccountAsync("x", TestUserId)).ReturnsAsync(false);

            var result = await _controller.DeleteAccount("x");

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
