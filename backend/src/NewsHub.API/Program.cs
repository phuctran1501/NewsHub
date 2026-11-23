using NewsHub.API;
using NewsHub.API.Extensions;
using NewsHub.API.Middleware;
using NewsHub.API.Services;
using NewsHub.Application;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure;

// Tự động load file .env nếu có ở thư mục gốc hoặc backend (Local Dev)
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

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

// Middleware Pipeline (thứ tự quan trọng)
// Exception handler đứng đầu để bắt lỗi toàn bộ pipeline bên dưới
app.UseExceptionHandler();

if (app.Environment.IsDevelopment()){app.MapOpenApi();}

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
