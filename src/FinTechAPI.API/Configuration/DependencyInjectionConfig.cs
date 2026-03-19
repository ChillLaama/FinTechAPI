using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using FinTechAPI.API.Auth;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Application.Mappings;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Payments;
using FinTechAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

namespace FinTechAPI.API.Configuration;

public static class DependencyInjectionConfig
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddControllers();
        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddEndpointsApiExplorer();
        services.AddHttpClient();

        // ── Firebase initialisation ──────────────────────────────────────
        var firebaseSection = configuration.GetSection("Firebase");

        var serviceAccountPath = firebaseSection["ServiceAccountPath"];
        GoogleCredential credential;

        // Resolve path relative to the executable directory
        if (!string.IsNullOrEmpty(serviceAccountPath))
        {
            var absolutePath = Path.IsPathRooted(serviceAccountPath)
                ? serviceAccountPath
                : Path.Combine(AppContext.BaseDirectory, serviceAccountPath);

            if (File.Exists(absolutePath))
                credential = GoogleCredential.FromFile(absolutePath);
            else
                throw new FileNotFoundException(
                    $"Firebase service account file not found at: {absolutePath}\n" +
                    $"Place 'firebase-service-account.json' in the API output directory or set an absolute path in appsettings.json.");
        }
        else
        {
            credential = GoogleCredential.GetApplicationDefault();
        }

        if (FirebaseApp.DefaultInstance == null)
            FirebaseApp.Create(new AppOptions { Credential = credential });

        var projectId = firebaseSection["ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId is not configured.");

        // Use FirestoreDbBuilder with explicit credential — avoids ADC lookup
        services.AddSingleton(_ => new FirestoreDbBuilder
        {
            ProjectId = projectId,
            Credential = credential.IsCreateScopedRequired
                ? credential.CreateScoped("https://www.googleapis.com/auth/datastore")
                : credential
        }.Build());
        services.AddSingleton<FirestoreProvider>();
        services.Configure<FirebaseSettings>(firebaseSection);

        // In production we require environment variables.
        // In development we allow fallback to appsettings for local convenience.
        var stripeApiKey = Environment.GetEnvironmentVariable("Stripe__ApiKey");
        var stripeWebhookSecret = Environment.GetEnvironmentVariable("Stripe__WebhookSecret");

        if (environment.IsDevelopment())
        {
            stripeApiKey ??= configuration["Stripe:ApiKey"];
            stripeWebhookSecret ??= configuration["Stripe:WebhookSecret"];
        }

        services.Configure<StripeSettings>(options =>
        {
            options.ApiKey = stripeApiKey ?? string.Empty;
            options.WebhookSecret = stripeWebhookSecret ?? string.Empty;
        });

        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(stripeApiKey))
                throw new InvalidOperationException("Missing required environment variable: Stripe__ApiKey");

            if (string.IsNullOrWhiteSpace(stripeWebhookSecret))
                throw new InvalidOperationException("Missing required environment variable: Stripe__WebhookSecret");
        }

        // ── Stripe ──────────────────────────────────────────────────────────
        services.AddScoped<IStripePaymentIntentService, StripePaymentIntentService>();
        services.AddScoped<IStripeBalanceService, StripeBalanceService>();

        // ── Application services ─────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPlatformBalanceService, PlatformBalanceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<ITransactionService, TransactionService>();

        // ── Authentication / Authorisation ───────────────────────────────
        services.AddAuthentication("Firebase")
            .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", null);
        services.AddAuthorization();

        // ── CORS ─────────────────────────────────────────────────────────
        services.AddCors(options =>
            options.AddPolicy("MauiPolicy", p =>
                p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        // ── Swagger ──────────────────────────────────────────────────────
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinTechAPI (Firebase)", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer {Firebase ID token}'"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }
}
