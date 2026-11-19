namespace NewsHub.API.Extensions;

using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;

public static class RateLimitingExtensions
{
    public const string LoginPolicy = "login-policy";
    public const string AiCategorizePolicy = "ai-categorize-policy";

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

                var isAiCategorize = context.HttpContext.Request.Path.Value?.Contains("/ai/categorize", StringComparison.OrdinalIgnoreCase) == true;
                var message = isAiCategorize
                    ? "Quá nhiều yêu cầu phân loại AI. Vui lòng thử lại sau 1 phút."
                    : "Quá nhiều yêu cầu. Vui lòng thử lại sau 1 phút.";

                var response = new ApiErrorResponse(
                    new ApiErrorDetail("TOO_MANY_REQUESTS", message, Array.Empty<ApiValidationErrorItem>()),
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

            options.AddPolicy(AiCategorizePolicy, httpContext =>
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var partitionKey = !string.IsNullOrWhiteSpace(userId)
                    ? $"user_{userId}"
                    : httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                        ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
