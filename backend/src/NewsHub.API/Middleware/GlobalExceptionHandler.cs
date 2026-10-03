using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace NewsHub.API.Middleware;

/// <summary>
/// Bắt toàn bộ unhandled exception, chuẩn hóa response theo RFC 7807 ProblemDetails.
/// Đăng ký qua AddExceptionHandler[GlobalExceptionHandler]() trong Program.cs.
/// 
/// Thứ tự xử lý:
///   NotFoundException         → 404
///   ConflictException         → 409
///   ForbiddenException        → 403
///   UnauthorizedAccessException → 401
///   ValidationException       → 400
///   Tất cả còn lại            → 500 (log chi tiết, ẩn trace với client)
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception, httpContext);

        // Log: chỉ log 5xx ở mức Error để tránh noise từ lỗi nghiệp vụ thông thường
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "Domain exception {ExceptionType} on {Method} {Path}: {Message}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Thêm traceId để developer dễ correlate với log
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        // Trả về true = exception đã được xử lý, không bubble up tiếp
        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(
        Exception exception,
        HttpContext httpContext)
    {
        return exception switch
        {
            NotFoundException ex =>
                (StatusCodes.Status404NotFound, "Resource Not Found", ex.Message),

            ConflictException ex =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message),

            ForbiddenException ex =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized",
                    "Authentication is required to access this resource."),

            ValidationException ex =>
                (StatusCodes.Status400BadRequest, "Validation Failed", ex.Message),

            // Catch-all: ẩn detail thật, chỉ trả về message an toàn
            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error",
                    "An unexpected error occurred. Please try again later.")
        };
    }
}
