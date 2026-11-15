namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ném khi dữ liệu đầu vào hoặc giá trị nghiệp vụ không thỏa mãn điều kiện hợp lệ tại tầng Domain.
/// Middleware sẽ tự động ánh xạ sang HTTP 422 Unprocessable Entity kèm danh sách errors.
/// </summary>
public sealed class ValidationException : AppException
{
    public ValidationException(string message)
        : base(message, AppErrorKind.Validation, ErrorCodes.ValidationFailed)
    {
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(
            "One or more validation failures have occurred.",
            AppErrorKind.Validation,
            ErrorCodes.ValidationFailed,
            new Dictionary<string, string[]>
            {
                [propertyName] = [errorMessage]
            })
    {
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", AppErrorKind.Validation, ErrorCodes.ValidationFailed, errors)
    {
    }
}
