namespace FinTechAPI.IntegrationTests.Fixtures;

/// <summary>
/// Defines a shared test collection so that all integration test classes
/// use a single <see cref="IntegrationTestFactory"/> instance (one WAF host per test run).
/// This avoids static-state conflicts (FirebaseApp, Serilog ReloadableLogger)
/// that arise when multiple WebApplicationFactory hosts are built in the same process.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFactory> { }
