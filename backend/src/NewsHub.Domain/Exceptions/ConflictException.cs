namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ném khi tạo hoặc sửa resource bị xung đột/trùng lặp (vd: email đã tồn tại, slug bài viết đã dùng).
/// Middleware sẽ tự động ánh xạ sang HTTP 409 Conflict.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string resourceName, string fieldName, object value)
        : base($"{resourceName} with {fieldName} '{value}' already exists.", AppErrorKind.Conflict, ErrorCodes.Conflict)
    {
    }

    public ConflictException(string message)
        : base(message, AppErrorKind.Conflict, ErrorCodes.Conflict)
    {
    }

    public ConflictException(string message, string errorCode)
        : base(message, AppErrorKind.Conflict, errorCode)
    {
    }
}
