namespace NewsHub.API.Extensions;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;

public static class RateLimitingExtensions
{
    public const string LoginPolicy = "login-policy";

    public static IServiceCollection AddAppRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                var correlationIdProvider = context.HttpContext.RequestServices.GetService<ICorrelationIdProvider>();
                var correlationId = correlationIdProvider?.GetCorrelationId() ?? context.HttpContext.TraceIdentifier;

                var response = new ApiErrorResponse(
                    new ApiErrorDetail("TOO_MANY_REQUESTS", "Quá nhiều lần thử đăng nhập thất bại. Vui lòng thử lại sau 1 phút.", Array.Empty<ApiValidationErrorItem>()),
                    correlationId);

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

            options.AddPolicy(LoginPolicy, httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
