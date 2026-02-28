using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace FinTechAPI.Tests
{
    public static class ControllerTestHelpers
    {
        public static HttpContext CreateHttpContext(string userId, string email, string role = "User")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.User).Returns(user);

            var requestCookiesMock = new Mock<IRequestCookieCollection>();
            var responseCookiesMock = new Mock<IResponseCookies>();
            httpContextMock.Setup(c => c.Request.Cookies).Returns(requestCookiesMock.Object);
            httpContextMock.Setup(c => c.Response.Cookies).Returns(responseCookiesMock.Object);

            return httpContextMock.Object;
        }
    }
}
