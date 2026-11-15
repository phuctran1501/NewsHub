namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Base class cho tất cả domain exceptions của NewsHub.
/// Kế thừa từ AppException để tích hợp với hệ thống phân loại mã lỗi thống nhất.
/// </summary>
public abstract class DomainException : AppException
{
    protected DomainException(
        string message,
        AppErrorKind errorKind = AppErrorKind.BadRequest,
        string errorCode = ErrorCodes.InvalidRequest,
        IDictionary<string, string[]>? errors = null)
        : base(message, errorKind, errorCode, errors)
    {
    }
}
