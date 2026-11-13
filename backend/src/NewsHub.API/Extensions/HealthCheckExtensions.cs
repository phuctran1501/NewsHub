using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace NewsHub.API.Extensions;

/// <summary>
/// Quản lý đăng ký, format phản hồi JSON và map các endpoint Health Check (/health, /health/live, /health/ready).
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."),
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db", "postgresql"])
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, tags: ["ready", "cache", "redis"]);

        return services;
    }

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Health check tổng thể / Readiness probe (Kiểm tra kết nối DB, Redis, etc.)
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        // Liveness probe (Kiểm tra tiến trình backend đang sống, không query DB)
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness probe rõ ràng
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        return endpoints;
    }

    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
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
}

/// <summary>
/// Health Check kiểm tra kết nối Redis.
/// Nếu Redis offline, trả về Degraded thay vì Unhealthy do hệ thống có cơ chế Fallback sang Database.
/// </summary>
public class RedisHealthCheck(IConnectionMultiplexer? multiplexer = null) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (multiplexer is not null && multiplexer.IsConnected)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Redis connection is active."));
            }

            return Task.FromResult(HealthCheckResult.Degraded("Redis is not connected. Application operating in database-fallback mode."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"Redis check failed: {ex.Message}. Operating in database-fallback mode."));
        }
    }
}
