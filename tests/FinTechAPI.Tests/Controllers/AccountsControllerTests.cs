using AutoMapper;
using FinTechAPI.API.Controllers;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinTechAPI.Tests
{
    public class AccountsControllerTests
    {
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly AccountsController _controller;
        private readonly string _testUserId = "test-user-id";
        private readonly string _testUserEmail = "test@example.com";

        public AccountsControllerTests()
        {
            _mockAccountService = new Mock<IAccountService>();
            _mockMapper = new Mock<IMapper>();

            _controller = new AccountsController(_mockAccountService.Object, _mockMapper.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = ControllerTestHelpers.CreateHttpContext(_testUserId, _testUserEmail)
            };
        }

        [Fact]
        public async Task GetAccounts_ShouldReturnOkObjectResult_WithListOfAccountDtos()
        {
            var accountsDto = new List<AccountDto>
            {
                new AccountDto { Id = 1, Name = "Checking", Balance = 100 },
                new AccountDto { Id = 2, Name = "Savings", Balance = 500 }
            };
            _mockAccountService.Setup(s => s.GetAccountsByUserIdAsync(_testUserId)).ReturnsAsync(accountsDto);

            var result = await _controller.GetAccounts();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<AccountDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count());
        }

        [Fact]
        public async Task GetAccount_ShouldReturnOkObjectResult_WhenAccountExists()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, Name = "Test Account", UserId = _testUserId, Balance = 100, AccountType = AccountType.Checking, Currency = Currency.USD };
            var accountDto = new AccountDto { Id = accountId, Name = "Test Account", Balance = 100 };

            _mockAccountService.Setup(s => s.GetAccountByIdAsync(accountId, _testUserId)).ReturnsAsync(account);
            _mockMapper.Setup(m => m.Map<AccountDto>(account)).Returns(accountDto);

            var result = await _controller.GetAccount(accountId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AccountDto>(okResult.Value);
            Assert.Equal(accountId, returnValue.Id);
        }

        [Fact]
        public async Task GetAccount_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            var accountId = 99;
            _mockAccountService.Setup(s => s.GetAccountByIdAsync(accountId, _testUserId)).ReturnsAsync((Account)null!);

            var result = await _controller.GetAccount(accountId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CreateAccount_ShouldReturnCreatedAtActionResult_WithCreatedAccount()
        {
            var accountToCreate = new Account { Name = "New Account", AccountType = AccountType.Savings, Balance = 0, Currency = Currency.EUR };
            var createdAccount = new Account { Id = 3, Name = accountToCreate.Name, AccountType = accountToCreate.AccountType, Balance = accountToCreate.Balance, Currency = accountToCreate.Currency, UserId = _testUserId };

            _mockAccountService.Setup(s => s.CreateAccountAsync(It.IsAny<Account>(), _testUserId)).ReturnsAsync(createdAccount);

            var result = await _controller.CreateAccount(accountToCreate);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(AccountsController.GetAccount), createdAtActionResult.ActionName);
            Assert.Equal(createdAccount.Id, createdAtActionResult.RouteValues["id"]);
            var returnValue = Assert.IsType<Account>(createdAtActionResult.Value);
            Assert.Equal(createdAccount.Id, returnValue.Id);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnNoContent_WhenUpdateIsSuccessful()
        {
            var accountId = 1;
            var accountUpdateDetails = new Account { Id = accountId, Name = "Updated Name", AccountType = AccountType.Checking, Balance = 200, Currency = Currency.USD };
            var updatedAccountFromService = new Account { Id = accountId, Name = "Updated Name", UserId = _testUserId };

            _mockAccountService.Setup(s => s.UpdateAccountAsync(accountId, It.Is<Account>(a => a.Id == accountId), _testUserId)).ReturnsAsync(updatedAccountFromService);

            var result = await _controller.UpdateAccount(accountId, accountUpdateDetails);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var accountId = 1;
            var accountUpdateDetails = new Account { Id = 2, Name = "Mismatch" };

            var result = await _controller.UpdateAccount(accountId, accountUpdateDetails);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnNotFound_WhenAccountToUpdateNotFound()
        {
            var accountId = 99;
            var accountUpdateDetails = new Account { Id = accountId, Name = "Non Existent" };
            _mockAccountService.Setup(s => s.UpdateAccountAsync(accountId, It.IsAny<Account>(), _testUserId)).ReturnsAsync((Account)null!);
            _mockAccountService.Setup(s => s.AccountExistsAsync(accountId, _testUserId)).ReturnsAsync(false);

            var result = await _controller.UpdateAccount(accountId, accountUpdateDetails);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteAccount_ShouldReturnNoContent_WhenDeleteIsSuccessful()
        {
            var accountId = 1;
            _mockAccountService.Setup(s => s.DeleteAccountAsync(accountId, _testUserId)).ReturnsAsync(true);

            var result = await _controller.DeleteAccount(accountId);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAccount_ShouldReturnNotFound_WhenAccountToDeleteNotFound()
        {
            var accountId = 99;
            _mockAccountService.Setup(s => s.DeleteAccountAsync(accountId, _testUserId)).ReturnsAsync(false);

            var result = await _controller.DeleteAccount(accountId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }
    }
}
