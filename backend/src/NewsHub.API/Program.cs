using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."),
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "postgresql"]);

// ── OpenAPI (Scalar) ──────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Development tools ─────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ── Health Check Endpoint ─────────────────────────────────────────────────────
// GET /health → 200 Healthy | 503 Unhealthy
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

app.Run();

// ── Response Writer ───────────────────────────────────────────────────────────
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
