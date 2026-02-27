using FinTechAPI.BusinessLogic;
using FinTechAPI.BusinessLogic.Authorization;
using FinTechAPI.Configuration;
using FinTechAPI.Data;
using FinTechAPI.DTOs;
using FinTechAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace FinTechAPI_Tests.BusinessLogic
{
    public class AuthServiceTests : IDisposable
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<IOptions<AuthSettings>> _mockAuthSettings;
        private readonly FinTechDbContext _dbContext;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Mock UserManager
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Mock SignInManager
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _mockSignInManager = new Mock<SignInManager<User>>(_mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            // Mock AuthSettings
            var authSettings = new AuthSettings { SecretKey = "TestSecretKeySuperLongAndSecureEnough", Issuer = "TestIssuer", Audience = "TestAudience", ExpirationInMinutes = 60 };
            _mockAuthSettings = new Mock<IOptions<AuthSettings>>();
            _mockAuthSettings.Setup(o => o.Value).Returns(authSettings);

            // In-memory DbContext
            var options = new DbContextOptionsBuilder<FinTechDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new FinTechDbContext(options);

            // Instantiate the service
            _authService = new AuthService(_mockUserManager.Object, _mockSignInManager.Object, _mockAuthSettings.Object, _dbContext);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUserAndAccount_WhenRegistrationSucceeds()
        {
            var registerDto = new RegisterUserDto { Email = "newuser@example.com", Password = "Password123!", FirstName = "New", LastName = "User" };
            
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<User, string>((user, password) => user.Id = "new-id"); // Set Id for created user

            _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            var (result, userDto) = await _authService.RegisterAsync(registerDto);

            Assert.True(result.Succeeded);
            Assert.NotNull(userDto);
            Assert.Equal(registerDto.Email, userDto.Email);

            var accountInDb = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == "new-id");
            Assert.NotNull(accountInDb);
            Assert.Equal("Main", accountInDb.Name);
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), "User"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFailedResult_WhenRegistrationFails()
        {
            var registerDto = new RegisterUserDto { Email = "fail@example.com", Password = "Weak" };
            var errors = new List<IdentityError> { new IdentityError { Code = "TestError", Description = "Test error description" } };
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            var (result, userDto) = await _authService.RegisterAsync(registerDto);

            Assert.False(result.Succeeded);
            Assert.Null(userDto);
            Assert.Contains(result.Errors, e => e.Code == "TestError");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
        {
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Id = "test-id", UserName = loginDto.Email, Email = loginDto.Email, FirstName = "Test", LastName = "User" };

            _mockUserManager.Setup(um => um.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(SignInResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            var authResponse = await _authService.LoginAsync(loginDto, "127.0.0.1");

            Assert.NotNull(authResponse);
            Assert.NotNull(authResponse.Token);
            Assert.NotEmpty(authResponse.Token);
            Assert.Equal(user.Email, authResponse.User.Email);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUserNotFound()
        {
            var loginDto = new LoginDto { Email = "wrong@example.com", Password = "password" };
            _mockUserManager.Setup(um => um.FindByEmailAsync(loginDto.Email)).ReturnsAsync((User)null);

            var result = await _authService.LoginAsync(loginDto, "127.0.0.1");

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
        {
            var loginDto = new LoginDto { Email = "test@example.com", Password = "wrong-password" };
            var user = new User { Id = "test-id", UserName = loginDto.Email, Email = loginDto.Email };

            _mockUserManager.Setup(um => um.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(SignInResult.Failed);

            var result = await _authService.LoginAsync(loginDto, "127.0.0.1");

            Assert.Null(result);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}