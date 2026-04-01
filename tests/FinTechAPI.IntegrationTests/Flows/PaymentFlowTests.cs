using System.Net;
using System.Net.Http.Json;
using FinTechAPI.Application.DTOs;
using FinTechAPI.Application.Exceptions;
using FinTechAPI.IntegrationTests.Fixtures;
using FinTechAPI.IntegrationTests.Helpers;
using Moq;

namespace FinTechAPI.IntegrationTests.Flows;

/// <summary>
/// End-to-end HTTP flow tests for the payments pipeline.
/// IPaymentService is mocked so tests focus on the HTTP layer:
/// routing, auth, (de)serialization, and controller error mapping.
/// </summary>
[Collection("Integration")]
public sealed class PaymentFlowTests
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public PaymentFlowTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient().AsUser();
    }

    // ── Create payment intent ─────────────────────────────────────────────

    [Fact]
    public async Task CreatePaymentIntent_HappyPath_Returns200WithBody()
    {
        var expected = new PaymentIntentResponseDto
        {
            PaymentId = "pay_test001",
            StripePaymentIntentId = "pi_test001",
            ClientSecret = "pi_test001_secret_xxx",
            Status = "requires_payment_method",
            Amount = 25.00m,
            Currency = "usd",
            FraudDecision = "Allow",
            FraudScore = 0,
        };

        _factory.PaymentService
            .Setup(s => s.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentDto>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(expected);

        var response = await _client.PostAsJsonAsync("/api/payments/intents",
            new { Amount = 25.00m, Currency = "usd" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("pay_test001", body.PaymentId);
        Assert.Equal("Allow", body.FraudDecision);
    }

    [Fact]
    public async Task CreatePaymentIntent_StripeDecline_Returns502()
    {
        _factory.PaymentService
            .Setup(s => s.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentDto>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new PaymentProviderException("Your card was declined.", "card_declined"));

        var response = await _client.PostAsJsonAsync("/api/payments/intents",
            new { Amount = 25.00m, Currency = "usd" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentIntent_InvalidBody_Returns400()
    {
        // Amount = 0 violates [Range(0.01, ...)] on CreatePaymentIntentDto
        var response = await _client.PostAsJsonAsync("/api/payments/intents",
            new { Amount = 0m, Currency = "usd" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Get payment by id ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPayment_Exists_Returns200()
    {
        _factory.PaymentService
            .Setup(s => s.GetPaymentByIdAsync("pay_test001", It.IsAny<string>()))
            .ReturnsAsync(new PaymentDto
            {
                Id = "pay_test001",
                Amount = 25.00m,
                Currency = "usd",
                Status = "succeeded",
            });

        var response = await _client.GetAsync("/api/payments/pay_test001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.NotNull(body);
        Assert.Equal("pay_test001", body.Id);
    }

    [Fact]
    public async Task GetPayment_NotFound_Returns404()
    {
        _factory.PaymentService
            .Setup(s => s.GetPaymentByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((PaymentDto?)null);

        var response = await _client.GetAsync("/api/payments/pay_missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentIntent_WithMlScoring_ReturnsMlFields()
    {
        var expected = new PaymentIntentResponseDto
        {
            PaymentId = "pay_ml_test",
            StripePaymentIntentId = "pi_ml_test",
            ClientSecret = "pi_ml_test_secret_xxx",
            Status = "requires_payment_method",
            Amount = 100.00m,
            Currency = "usd",
            FraudDecision = "Allow",
            FraudScore = 10,
        };

        _factory.PaymentService
            .Setup(s => s.CreatePaymentIntentAsync(
                It.IsAny<CreatePaymentIntentDto>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(expected);

        var response = await _client.PostAsJsonAsync("/api/payments/intents",
            new { Amount = 100.00m, Currency = "usd" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaymentIntentResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("pay_ml_test", body.PaymentId);
        Assert.Equal("Allow", body.FraudDecision);
    }
}

/// <summary>
/// Fraud case management flow tests (admin role required).
/// </summary>
[Collection("Integration")]
public sealed class FraudCaseFlowTests
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _admin;
    private readonly HttpClient _user;

    public FraudCaseFlowTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _admin = factory.CreateClient().AsAdmin();
        _user = factory.CreateClient().AsUser();
    }

    [Fact]
    public async Task GetFraudCaseById_AdminRole_NotFound_Returns404()
    {
        _factory.FraudCaseService
            .Setup(s => s.GetCaseByIdAsync("case_missing"))
            .ReturnsAsync((FraudCaseDto?)null);

        var response = await _admin.GetAsync("/api/fraud-cases/case_missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFraudCaseById_AdminRole_Found_Returns200()
    {
        _factory.FraudCaseService
            .Setup(s => s.GetCaseByIdAsync("case_001"))
            .ReturnsAsync(new FraudCaseDto
            {
                Id = "case_001",
                Status = "open",
                RiskLevel = "High",
                FraudScore = 60,
            });

        var response = await _admin.GetAsync("/api/fraud-cases/case_001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApproveFraudCase_AdminRole_Returns200()
    {
        _factory.FraudCaseService
            .Setup(s => s.ApproveCaseAsync(
                "case_001",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new FraudCaseDto { Id = "case_001", Status = "approved" });

        var response = await _admin.PostAsJsonAsync("/api/fraud-cases/case_001/approve",
            new { AnalystNotes = "Legitimate transaction." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApproveFraudCase_UserRole_Returns403()
    {
        var response = await _user.PostAsJsonAsync("/api/fraud-cases/case_001/approve",
            new { AnalystNotes = "Trying to approve." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFraudCases_Pagination_LimitClamped()
    {
        // limit=200 should be clamped to 20 inside the controller
        _factory.FraudCaseService
            .Setup(s => s.GetCasesAsync(null, 20, null))
            .ReturnsAsync(new FraudCasePageDto { Items = [], TotalCount = 0 });

        var response = await _admin.GetAsync("/api/fraud-cases?limit=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.FraudCaseService.Verify(
            s => s.GetCasesAsync(null, 20, null),
            Times.Once);
    }
}
