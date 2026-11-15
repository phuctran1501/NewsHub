using Microsoft.AspNetCore.Diagnostics;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

namespace NewsHub.API.Middleware;

/// <summary>
/// Xử lý ngoại lệ tập trung cho toàn bộ ứng dụng, chuẩn hóa định dạng JSON phản hồi lỗi:
/// { "error": { "code": string, "message": string, "details": [...] }, "correlationId": string }
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdProvider.GetCorrelationId();
        var (statusCode, errorCode, message, validationDetails) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled server exception on {Method} {Path}. CorrelationId: {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                "API exception {ExceptionType} ({StatusCode} {ErrorCode}) on {Method} {Path}: {Message}. CorrelationId: {CorrelationId}",
                exception.GetType().Name,
                statusCode,
                errorCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                message,
                correlationId);
        }

        var response = new ApiErrorResponse(
            new ApiErrorDetail(errorCode, message, validationDetails),
            correlationId);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static (int StatusCode, string ErrorCode, string Message, IReadOnlyList<ApiValidationErrorItem> Details) MapException(Exception exception)
    {
        if (exception is AppException appException)
        {
            var details = new List<ApiValidationErrorItem>();
            if (appException.Errors != null)
            {
                foreach (var (field, messages) in appException.Errors)
                {
                    if (messages == null)
                    {
                        continue;
                    }

                    foreach (var msg in messages)
                    {
                        details.Add(new ApiValidationErrorItem(field, msg));
                    }
                }
            }

            return ((int)appException.ErrorKind, appException.ErrorCode, appException.Message, details);
        }

        if (exception is UnauthorizedAccessException unauth)
        {
            return (
                StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized,
                string.IsNullOrWhiteSpace(unauth.Message) ? "Authentication is required to access this resource." : unauth.Message,
                Array.Empty<ApiValidationErrorItem>());
        }

        return (
            StatusCodes.Status500InternalServerError,
            ErrorCodes.InternalServerError,
            "An unexpected error occurred. Please try again later.",
            Array.Empty<ApiValidationErrorItem>());
    }
}
