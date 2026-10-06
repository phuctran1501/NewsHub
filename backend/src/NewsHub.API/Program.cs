using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NewsHub.API;
using NewsHub.API.Middleware;
using NewsHub.Infrastructure;
using System.Text;
using System.Text.Json;

// Tự động load file .env nếu có ở thư mục gốc hoặc backend (Local Dev)
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// ── Exception Handling ────────────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Database & Infrastructure (EF Core PostgreSQL Supabase) ───────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── CORS ──────────────────────────────────────────────────────────────────────
const string FrontendCorsPolicy = "FrontendCorsPolicy";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "https://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."),
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "postgresql"]);

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware Pipeline (thứ tự rất quan trọng) ───────────────────────────────
// 1. Exception handler phải đứng ĐẦU TIÊN để bắt lỗi từ toàn bộ pipeline bên dưới
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 2. CORS phải đứng trước Authentication, Authorization và Endpoint mapping
//    để các preflight OPTIONS request được phản hồi sớm với header hợp lệ
app.UseCors(FrontendCorsPolicy);

// ── Endpoints ─────────────────────────────────────────────────────────────────
// GET /health → 200 Healthy | 503 Unhealthy
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var result = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds + "ms"
        }),
        totalDuration = report.TotalDuration.TotalMilliseconds + "ms"
    };

    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    return context.Response.WriteAsync(json, Encoding.UTF8);
}
