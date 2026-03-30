using FinTechAPI.Application.Interfaces;
using FinTechAPI.Infrastructure.Payments;
using FinTechAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FinTechAPI.IntegrationTests.Fixtures;

/// <summary>
/// <para>
///   Shared <see cref="WebApplicationFactory{TEntryPoint}"/> for all integration tests.
///   Starts the real ASP.NET Core pipeline but replaces every external dependency
///   (Stripe, Firestore, Firebase auth) with in-memory test doubles.
/// </para>
/// <para>
///   <b>Pre-requisite:</b> <c>firebase-service-account.json</c> must exist in the
///   <c>FinTechAPI.API</c> source directory so it is copied to the test output folder.
///   The file is only needed to satisfy the startup credential-loading code; no real
///   Firebase calls are made because all services are replaced via <c>ConfigureTestServices</c>.
/// </para>
/// </summary>
public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    // ── Exposed mocks ────────────────────────────────────────────────────────
    public Mock<IFraudService> FraudService { get; } = new(MockBehavior.Loose);
    public Mock<IPaymentService> PaymentService { get; } = new(MockBehavior.Loose);
    public Mock<IFraudCaseService> FraudCaseService { get; } = new(MockBehavior.Loose);
    public Mock<IStripePaymentIntentService> StripePaymentIntentService { get; } = new(MockBehavior.Loose);
    public Mock<IStripeBalanceService> StripeBalanceService { get; } = new(MockBehavior.Loose);
    public Mock<IAuditService> AuditService { get; } = new(MockBehavior.Loose);

    /// <summary>Clears all mock setups so tests in different classes don't interfere.</summary>
    public void ResetMocks()
    {
        FraudService.Reset();
        PaymentService.Reset();
        FraudCaseService.Reset();
        StripePaymentIntentService.Reset();
        StripeBalanceService.Reset();
        AuditService.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Run as Development so Swagger is available and Stripe key validation is skipped.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // ── Remove background service that would contact Firestore ─────
            var bg = services.FirstOrDefault(
                d => d.ImplementationType == typeof(ReconciliationBackgroundService));
            if (bg != null) services.Remove(bg);

            // ── Replace the Firebase auth handler with a test stub ─────────
            //    We patch the already-registered "Firebase" scheme in-place so
            //    the AuthenticationOptions pipeline stays intact.
            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                var scheme = opts.Schemes.FirstOrDefault(s => s.Name == "Firebase");
                if (scheme != null)
                    scheme.HandlerType = typeof(FakeFirebaseAuthHandler);
            });
            services.AddTransient<FakeFirebaseAuthHandler>();

            // ── Replace Stripe adapters ────────────────────────────────────
            services.RemoveAll<IStripePaymentIntentService>();
            services.AddScoped<IStripePaymentIntentService>(_ => StripePaymentIntentService.Object);

            services.RemoveAll<IStripeBalanceService>();
            services.AddScoped<IStripeBalanceService>(_ => StripeBalanceService.Object);

            // ── Replace application services ──────────────────────────────
            services.RemoveAll<IFraudService>();
            services.AddScoped<IFraudService>(_ => FraudService.Object);

            services.RemoveAll<IPaymentService>();
            services.AddScoped<IPaymentService>(_ => PaymentService.Object);

            services.RemoveAll<IFraudCaseService>();
            services.AddScoped<IFraudCaseService>(_ => FraudCaseService.Object);

            services.RemoveAll<IAuditService>();
            services.AddScoped<IAuditService>(_ => AuditService.Object);
        });
    }
}
