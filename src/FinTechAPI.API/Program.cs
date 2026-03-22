using FinTechAPI.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

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
