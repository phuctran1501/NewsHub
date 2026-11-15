namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ngoại lệ ném ra khi request từ client không hợp lệ về mặt tham số hoặc cấu trúc logic.
/// Middleware sẽ tự động ánh xạ sang HTTP 400 Bad Request.
/// </summary>
public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(message, AppErrorKind.BadRequest, ErrorCodes.InvalidRequest)
    {
    }

    public BadRequestException(string message, string errorCode)
        : base(message, AppErrorKind.BadRequest, errorCode)
    {
    }

    public BadRequestException(string message, IDictionary<string, string[]> errors)
        : base(message, AppErrorKind.BadRequest, ErrorCodes.InvalidRequest, errors)
    {
    }

    public BadRequestException(string message, string errorCode, IDictionary<string, string[]> errors)
        : base(message, AppErrorKind.BadRequest, errorCode, errors)
    {
    }
}
