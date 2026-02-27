using AutoMapper;
using Moq;
using FinTechAPI.Controllers;
using FinTechAPI.Services;
using FinTechAPI.Models;
using Microsoft.AspNetCore.Mvc;

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
            // Arrange
            var accountsDto = new List<AccountDto>
            {
                new AccountDto { Id = 1, Name = "Checking", Balance = 100 },
                new AccountDto { Id = 2, Name = "Savings", Balance = 500 }
            };
            _mockAccountService.Setup(s => s.GetAccountsByUserIdAsync(_testUserId))
                .ReturnsAsync(accountsDto);

            // Act
            var result = await _controller.GetAccounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<AccountDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count());
        }

        [Fact]
        public async Task GetAccount_ShouldReturnOkObjectResult_WhenAccountExists()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, Name = "Test Account", UserId = _testUserId, Balance = 100, AccountType = AccountType.Checking, Currency = Currency.USD };
            var accountDto = new AccountDto { Id = accountId, Name = "Test Account", Balance = 100 };

            _mockAccountService.Setup(s => s.GetAccountByIdAsync(accountId, _testUserId))
                .ReturnsAsync(account);
            _mockMapper.Setup(m => m.Map<AccountDto>(account))
                .Returns(accountDto);

            // Act
            var result = await _controller.GetAccount(accountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AccountDto>(okResult.Value);
            Assert.Equal(accountId, returnValue.Id);
        }

        [Fact]
        public async Task GetAccount_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            var accountId = 99;
            _mockAccountService.Setup(s => s.GetAccountByIdAsync(accountId, _testUserId))
                .ReturnsAsync((Account)null!); // Возвращаем null, если аккаунт не найден

            // Act
            var result = await _controller.GetAccount(accountId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value); // У вас есть сообщение в NotFound
        }

        [Fact]
        public async Task CreateAccount_ShouldReturnCreatedAtActionResult_WithCreatedAccount()
        {
            // Arrange
            var accountToCreate = new Account { Name = "New Account", AccountType = AccountType.Savings, Balance = 0, Currency = Currency.EUR };
            var createdAccount = new Account { Id = 3, Name = accountToCreate.Name, AccountType = accountToCreate.AccountType, Balance = accountToCreate.Balance, Currency = accountToCreate.Currency, UserId = _testUserId };
            
            _mockAccountService.Setup(s => s.CreateAccountAsync(It.IsAny<Account>(), _testUserId))
                .ReturnsAsync(createdAccount);

            // Act
            var result = await _controller.CreateAccount(accountToCreate);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(AccountsController.GetAccount), createdAtActionResult.ActionName);
            Assert.Equal(createdAccount.Id, createdAtActionResult.RouteValues["id"]);
            var returnValue = Assert.IsType<Account>(createdAtActionResult.Value);
            Assert.Equal(createdAccount.Id, returnValue.Id);
        }
        
        [Fact]
        public async Task UpdateAccount_ShouldReturnNoContent_WhenUpdateIsSuccessful()
        {
            // Arrange
            var accountId = 1;
            var accountUpdateDetails = new Account { Id = accountId, Name = "Updated Name", AccountType = AccountType.Checking, Balance = 200, Currency = Currency.USD };
            var updatedAccountFromService = new Account { Id = accountId, Name = "Updated Name", AccountType = AccountType.Checking, Balance = 150, Currency = Currency.USD, UserId = _testUserId }; // Сервис может возвращать обновленный объект

            _mockAccountService.Setup(s => s.UpdateAccountAsync(accountId, It.Is<Account>(a => a.Id == accountId), _testUserId))
                .ReturnsAsync(updatedAccountFromService);

            // Act
            var result = await _controller.UpdateAccount(accountId, accountUpdateDetails);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnBadRequest_WhenIdMismatch()
        {
            // Arrange
            var accountId = 1;
            var accountUpdateDetails = new Account { Id = 2, Name = "Mismatch" }; // ID не совпадает

            // Act
            var result = await _controller.UpdateAccount(accountId, accountUpdateDetails);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
             Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateAccount_ShouldReturnNotFound_WhenAccountToUpdateNotFound()
        {
            // Arrange
            var accountId = 99;
            var accountUpdateDetails = new Account { Id = accountId, Name = "Non Existent" };
            _mockAccountService.Setup(s => s.UpdateAccountAsync(accountId, It.IsAny<Account>(), _testUserId))
                .ReturnsAsync((Account)null!); // Сервис не нашел аккаунт
            _mockAccountService.Setup(s => s.AccountExistsAsync(accountId, _testUserId))
                .ReturnsAsync(false); // И аккаунт действительно не существует

            // Act
            var result = await _controller.UpdateAccount(accountId, accountUpdateDetails);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }


        [Fact]
        public async Task DeleteAccount_ShouldReturnNoContent_WhenDeleteIsSuccessful()
        {
            // Arrange
            var accountId = 1;
            _mockAccountService.Setup(s => s.DeleteAccountAsync(accountId, _testUserId))
                .ReturnsAsync(true); // Успешное удаление

            // Act
            var result = await _controller.DeleteAccount(accountId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAccount_ShouldReturnNotFound_WhenAccountToDeleteNotFound()
        {
            // Arrange
            var accountId = 99;
            _mockAccountService.Setup(s => s.DeleteAccountAsync(accountId, _testUserId))
                .ReturnsAsync(false); // Неудачное удаление (аккаунт не найден)

            // Act
            var result = await _controller.DeleteAccount(accountId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }
    }
}