namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ném khi user không có quyền truy cập resource (đã xác thực nhưng không đủ quyền hạn).
/// Middleware sẽ tự động ánh xạ sang HTTP 403 Forbidden.
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException()
        : base("You do not have permission to perform this action.", AppErrorKind.Forbidden, ErrorCodes.Forbidden)
    {
    }

    public ForbiddenException(string message)
        : base(message, AppErrorKind.Forbidden, ErrorCodes.Forbidden)
    {
    }

    public ForbiddenException(string message, string errorCode)
        : base(message, AppErrorKind.Forbidden, errorCode)
    {
    }
}
