namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ném khi một resource được yêu cầu không tồn tại trong hệ thống.
/// Middleware sẽ tự động ánh xạ sang HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.", AppErrorKind.NotFound, ErrorCodes.ResourceNotFound)
    {
    }

    public NotFoundException(string resourceName, object key, string errorCode)
        : base($"{resourceName} with key '{key}' was not found.", AppErrorKind.NotFound, errorCode)
    {
    }

    public NotFoundException(string message)
        : base(message, AppErrorKind.NotFound, ErrorCodes.ResourceNotFound)
    {
    }

    public static NotFoundException WithErrorCode(string message, string errorCode)
        => new(message, errorCode);

    private NotFoundException(string message, string errorCode)
        : base(message, AppErrorKind.NotFound, errorCode)
    {
    }
}
