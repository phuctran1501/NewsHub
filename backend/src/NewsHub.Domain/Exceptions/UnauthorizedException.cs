namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ngoại lệ ném ra khi request yêu cầu xác thực nhưng token bị thiếu, hết hạn hoặc không hợp lệ.
/// Middleware sẽ tự động ánh xạ sang HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication is required to access this resource.")
        : base(message, AppErrorKind.Unauthorized, ErrorCodes.Unauthorized)
    {
    }

    public UnauthorizedException(string message, string errorCode)
        : base(message, AppErrorKind.Unauthorized, errorCode)
    {
    }
}
