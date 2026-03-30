using FinTechAPI.IntegrationTests.Fixtures;

namespace FinTechAPI.IntegrationTests.Helpers;

/// <summary>
/// Extension methods that attach test identity headers to an <see cref="HttpClient"/>.
/// </summary>
public static class AuthHelper
{
    /// <summary>Configures the client to authenticate as an admin user.</summary>
    public static HttpClient AsAdmin(this HttpClient client, string userId = "admin-001")
    {
        client.DefaultRequestHeaders.Remove(FakeFirebaseAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(FakeFirebaseAuthHandler.RoleHeader);
        client.DefaultRequestHeaders.Add(FakeFirebaseAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(FakeFirebaseAuthHandler.RoleHeader, "admin");
        return client;
    }

    /// <summary>Configures the client to authenticate as a regular user.</summary>
    public static HttpClient AsUser(this HttpClient client, string userId = "user-001")
    {
        client.DefaultRequestHeaders.Remove(FakeFirebaseAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(FakeFirebaseAuthHandler.RoleHeader);
        client.DefaultRequestHeaders.Add(FakeFirebaseAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(FakeFirebaseAuthHandler.RoleHeader, "user");
        return client;
    }
}
