namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Lớp cơ sở (Base Exception) cho toàn bộ ngoại lệ nghiệp vụ trong hệ thống NewsHub.
/// Chứa thông tin phân loại mã lỗi HTTP (AppErrorKind), mã lỗi định danh (ErrorCode)
/// và danh sách chi tiết lỗi trường dữ liệu (Errors).
/// </summary>
public abstract class AppException : Exception
{
    public AppErrorKind ErrorKind { get; }
    public string ErrorCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    protected AppException(
        string message,
        AppErrorKind errorKind = AppErrorKind.BadRequest,
        string errorCode = ErrorCodes.InvalidRequest,
        IDictionary<string, string[]>? errors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorKind = errorKind;
        ErrorCode = errorCode;
        Errors = errors;
    }
}
