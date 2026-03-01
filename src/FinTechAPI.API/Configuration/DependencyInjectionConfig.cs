using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using FinTechAPI.API.Auth;
using FinTechAPI.Application.Interfaces;
using FinTechAPI.Application.Mappings;
using FinTechAPI.Infrastructure.Firebase;
using FinTechAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

namespace FinTechAPI.API.Configuration;

public static class DependencyInjectionConfig
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddEndpointsApiExplorer();
        services.AddHttpClient();

        // ── Firebase initialisation ──────────────────────────────────────
        var firebaseSection = configuration.GetSection("Firebase");

        if (FirebaseApp.DefaultInstance == null)
        {
            var serviceAccountPath = firebaseSection["ServiceAccountPath"];
            GoogleCredential credential;

            if (!string.IsNullOrEmpty(serviceAccountPath) && File.Exists(serviceAccountPath))
                credential = GoogleCredential.FromFile(serviceAccountPath);
            else
                credential = GoogleCredential.GetApplicationDefault();

            FirebaseApp.Create(new AppOptions { Credential = credential });
        }

        var projectId = firebaseSection["ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId is not configured.");

        services.AddSingleton(_ => FirestoreDb.Create(projectId));
        services.AddSingleton<FirestoreProvider>();
        services.Configure<FirebaseSettings>(firebaseSection);

        // ── Application services ─────────────────────────────────────────
        services.AddScoped<IAuthService,        AuthService>();
        services.AddScoped<IAccountService,     AccountService>();
        services.AddScoped<IReportingService,   ReportingService>();
        services.AddScoped<ISecurityService,    SecurityService>();
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
                Name        = "Authorization",
                Type        = SecuritySchemeType.ApiKey,
                Scheme      = "Bearer",
                BearerFormat = "JWT",
                In          = ParameterLocation.Header,
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
