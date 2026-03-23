using FinTechAPI.API.Configuration;
using FinTechAPI.API.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}      {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FinTechAPI")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}      {Message:lj}{NewLine}{Exception}"));

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddJsonFile("appsettings_Dev.json", optional: true, reloadOnChange: true);
    }

    builder.Services.AddServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinTechAPI v1");
            c.RoutePrefix = string.Empty;
            c.EnablePersistAuthorization();       // token survives page refresh
            c.InjectJavascript("/swagger-auto-auth.js"); // auto-insert token after login
        });
    }

    app.UseStaticFiles(); // serves wwwroot (including swagger-auto-auth.js)

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId) && correlationId is not null)
                diagnosticContext.Set("CorrelationId", correlationId);
        };
    });

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // Global error handling — prevents stack trace leaks in production
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;
            var isDev = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            var message = isDev ? "An internal server error occurred." : "Internal server error.";
            await context.Response.WriteAsJsonAsync(new { message });
        });
    });

    app.UseRateLimiter();
    app.UseCors("MauiPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
