using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using FinTechAPI.API.Auth;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Application.Mappings;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.ML;
using FinTechAPI.Infrastructure.Payments;
using FinTechAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
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
            else if (environment.IsProduction())
                throw new FileNotFoundException(
                    $"Firebase service account file not found at: {absolutePath}\n" +
                    $"Place 'firebase-service-account.json' in the API output directory or set an absolute path in appsettings.json.");
            else
                // In non-production (dev/test) all Firebase services are replaced by test doubles;
                // use a placeholder access token so startup succeeds without real credentials.
                credential = GoogleCredential.FromAccessToken("_test_placeholder_");
        }
        else
        {
            try
            {
                credential = GoogleCredential.GetApplicationDefault();
            }
            catch when (!environment.IsProduction())
            {
                credential = GoogleCredential.FromAccessToken("_test_placeholder_");
            }
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            try { FirebaseApp.Create(new AppOptions { Credential = credential }); }
            catch (ArgumentException) { /* already exists — parallel test host builds */ }
        }

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
        services.Configure<FraudMlSettings>(configuration.GetSection("FraudMl"));
        services.AddSingleton<IFraudMlService, MlNetFraudScoringService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPlatformBalanceService, PlatformBalanceService>();
        services.AddScoped<IPlatformSummaryService, PlatformSummaryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IFraudService, FraudRuleEngine>();
        services.AddScoped<IFraudCaseService, FraudCaseService>();
        services.AddScoped<INotificationService, NotificationService>();

        // ── Background services ─────────────────────────────────────────
        services.AddHostedService<ReconciliationBackgroundService>();

        // ── Authentication / Authorisation ───────────────────────────────
        services.AddAuthentication("Firebase")
            .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", null);
        services.AddAuthorization();

        // ── Rate Limiting ────────────────────────────────────────────────
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddFixedWindowLimiter("fixed", limiter =>
            {
                limiter.PermitLimit = 100;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("auth", limiter =>
            {
                limiter.PermitLimit = 20;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
        });

        // ── CORS ─────────────────────────────────────────────────────────
        services.AddCors(options =>
            options.AddPolicy("MauiPolicy", p =>
            {
                if (environment.IsDevelopment())
                {
                    p.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5000")
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
                }
                else
                {
                    p.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials();
                }
            }));

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
