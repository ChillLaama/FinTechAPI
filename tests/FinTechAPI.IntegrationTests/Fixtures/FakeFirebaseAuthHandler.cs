using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTechAPI.IntegrationTests.Fixtures;

/// <summary>
/// Replaces the real FirebaseAuthenticationHandler in integration tests.
/// Authenticates any request that carries the <see cref="UserIdHeader"/> header.
/// </summary>
public sealed class FakeFirebaseAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Header that carries the test user ID (e.g. "test-user-001").</summary>
    public const string UserIdHeader = "X-Test-UserId";

    /// <summary>Header that carries the test role (e.g. "admin" or "user").</summary>
    public const string RoleHeader = "X-Test-Role";

    public FakeFirebaseAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = userIdValues.ToString();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers.TryGetValue(RoleHeader, out var roleValues)
            ? roleValues.ToString()
            : "user";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, $"{userId}@test.invalid"),
            new Claim(ClaimTypes.Role, role),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
