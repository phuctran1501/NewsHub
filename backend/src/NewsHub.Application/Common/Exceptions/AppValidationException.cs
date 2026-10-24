using FluentValidation.Results;

namespace NewsHub.Application.Common.Exceptions;

/// <summary>
/// Ngoại lệ xác thực dữ liệu đầu vào tại tầng Application (FluentValidation).
/// Middleware sẽ bắt và map sang HTTP 422 Unprocessable Entity kèm danh sách errors chi tiết.
/// </summary>
public class AppValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public AppValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public AppValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public AppValidationException(string propertyName, string errorMessage)
        : this()
    {
        Errors[propertyName] = [errorMessage];
    }

    public AppValidationException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }
}
