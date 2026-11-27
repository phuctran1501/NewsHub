using Microsoft.AspNetCore.HttpOverrides;
using NewsHub.API;
using NewsHub.API.Extensions;
using NewsHub.API.Middleware;
using NewsHub.API.Services;
using NewsHub.Application;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure;
using NewsHub.Infrastructure.Persistence;

// Tự động load file .env nếu có ở thư mục gốc hoặc backend (Local Dev)
var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.IsNullOrEmpty(aspnetEnv) || string.Equals(aspnetEnv, "Development", StringComparison.OrdinalIgnoreCase))
{
    DotEnvLoader.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Dynamic PORT binding cho Cloud Hosts (Render cấp động qua biến môi trường PORT)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// Exception Handling
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// Application & Infrastructure Layers
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Web Features (CORS, Authentication, SignalR, Health Checks, OpenAPI, Hangfire, Controllers)
builder.Services.AddControllers();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSignalRServices();
builder.Services.AddAppHealthChecks(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddAppRateLimiter();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment() && args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase))
{
    await DevelopmentDataSeeder.SeedAsync(app.Services);
    return;
}

// Forwarded Headers cho môi trường Reverse Proxy Cloud (Render / Cloudflare)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Middleware Pipeline (thứ tự quan trọng)
// Exception handler đứng đầu để bắt lỗi toàn bộ pipeline bên dưới
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableOpenApi"))
{
    app.MapOpenApi();
}

if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

// CORS đứng trước Auth và Endpoint mapping để phản hồi sớm preflight OPTIONS
app.UseCorsPolicy();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard & Recurring Jobs
app.UseHangfireDashboardCustom(app.Configuration);
app.UseHangfireRecurringJobs();

// Rate Limiting
app.UseRateLimiter();

// Endpoints & SignalR Hubs
app.MapControllers();
app.MapSignalRHubs();
app.MapAppHealthChecks();

app.Run();
