namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ngoại lệ ném ra khi tài nguyên hoặc phiên làm việc đã bị thu hồi vĩnh viễn và không còn khả dụng (vd: Refresh Token bị Revoked/Reused).
/// Middleware sẽ tự động ánh xạ sang HTTP 410 Gone.
/// </summary>
public class GoneException : AppException
{
    public GoneException(string message = "The requested resource or session has been revoked and is no longer available.")
        : base(message, AppErrorKind.Gone, ErrorCodes.TokenRevoked)
    {
    }

    public GoneException(string message, string errorCode)
        : base(message, AppErrorKind.Gone, errorCode)
    {
    }
}
