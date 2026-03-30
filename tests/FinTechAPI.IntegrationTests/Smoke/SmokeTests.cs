using System.Net;
using System.Net.Http.Json;
using FinTechAPI.Application.DTOs;
using FinTechAPI.IntegrationTests.Fixtures;
using FinTechAPI.IntegrationTests.Helpers;
using Moq;

namespace FinTechAPI.IntegrationTests.Smoke;

/// <summary>
/// Smoke tests that verify auth boundaries and basic HTTP contract of key endpoints.
/// These tests assert the correct HTTP status codes WITHOUT exercising business logic
/// (all application services are replaced with mocks in <see cref="IntegrationTestFactory"/>).
/// </summary>
[Collection("Integration")]
public sealed class SmokeTests
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _anon;
    private readonly HttpClient _user;
    private readonly HttpClient _admin;

    public SmokeTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _anon = factory.CreateClient();
        _user = factory.CreateClient().AsUser();
        _admin = factory.CreateClient().AsAdmin();
    }

    // ── Payments ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPayment_Anonymous_Returns401()
    {
        var response = await _anon.GetAsync("/api/payments/pay_any");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentIntent_Anonymous_Returns401()
    {
        var response = await _anon.PostAsJsonAsync("/api/payments/intents",
            new { Amount = 10.00m, Currency = "usd" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Fraud Cases ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetFraudCases_Anonymous_Returns401()
    {
        var response = await _anon.GetAsync("/api/fraud-cases");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFraudCases_UserRole_Returns403()
    {
        var response = await _user.GetAsync("/api/fraud-cases");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFraudCases_AdminRole_Returns200()
    {
        _factory.FraudCaseService
            .Setup(s => s.GetCasesAsync(
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync(new FraudCasePageDto { Items = [], TotalCount = 0 });

        var response = await _admin.GetAsync("/api/fraud-cases");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Accounts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccounts_Anonymous_Returns401()
    {
        var response = await _anon.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Swagger ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SwaggerJson_Returns200()
    {
        var response = await _anon.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
