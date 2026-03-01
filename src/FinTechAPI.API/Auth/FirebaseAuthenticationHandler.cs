using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTechAPI.API.Auth
{
    public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public FirebaseAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = GetTokenFromRequest();
            if (string.IsNullOrEmpty(token))
                return AuthenticateResult.NoResult();

            try
            {
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, decoded.Uid),
                };

                if (decoded.Claims.TryGetValue("email", out var email))
                    claims.Add(new Claim(ClaimTypes.Email, email.ToString()!));

                if (decoded.Claims.TryGetValue("role", out var role))
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()!));

                var identity  = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket    = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail($"Firebase token validation failed: {ex.Message}");
            }
        }

        private string? GetTokenFromRequest()
        {
            // Check Authorization cookie first (for browser/MAUI clients using cookie auth)
            var cookieToken = Request.Cookies["Authorization"];
            if (!string.IsNullOrEmpty(cookieToken))
                return cookieToken;

            // Fallback to Authorization header Bearer token
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                return authHeader["Bearer ".Length..].Trim();

            return null;
        }
    }
}
