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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Application & Infrastructure Layers
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Web Features (CORS, Health Checks, OpenAPI, Hangfire)
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddAppHealthChecks(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware Pipeline (thứ tự quan trọng)
// Exception handler đứng đầu để bắt lỗi toàn bộ pipeline bên dưới
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// CORS đứng trước Auth và Endpoint mapping để phản hồi sớm preflight OPTIONS
app.UseCorsPolicy();

// Hangfire Dashboard
app.UseHangfireDashboardCustom(app.Configuration);

// Endpoints
app.MapAppHealthChecks();

app.Run();
