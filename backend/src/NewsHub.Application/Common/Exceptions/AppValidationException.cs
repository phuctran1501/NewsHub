using FluentValidation.Results;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

namespace NewsHub.Application.Common.Exceptions;

/// <summary>
/// Ngoại lệ xác thực dữ liệu đầu vào tại tầng Application (FluentValidation).
/// Middleware sẽ bắt và map sang HTTP 422 Unprocessable Entity kèm danh sách errors chi tiết.
/// Kế thừa từ AppException với AppErrorKind.Validation.
/// </summary>
public class AppValidationException : AppException
{
    public new IDictionary<string, string[]> Errors => base.Errors ?? new Dictionary<string, string[]>();

    public AppValidationException()
        : base("One or more validation failures have occurred.", AppErrorKind.Validation, ErrorCodes.ValidationFailed, new Dictionary<string, string[]>())
    {
    }

    public AppValidationException(IEnumerable<ValidationFailure> failures)
        : base(
            "One or more validation failures have occurred.",
            AppErrorKind.Validation,
            ErrorCodes.ValidationFailed,
            failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray()))
    {
    }

    public AppValidationException(string propertyName, string errorMessage)
        : base(
            errorMessage,
            AppErrorKind.Validation,
            ErrorCodes.ValidationFailed,
            new Dictionary<string, string[]>
            {
                [propertyName] = [errorMessage]
            })
    {
    }

    public AppValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", AppErrorKind.Validation, ErrorCodes.ValidationFailed, errors)
    {
    }
}
