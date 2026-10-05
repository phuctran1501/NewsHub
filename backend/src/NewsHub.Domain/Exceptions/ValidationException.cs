namespace NewsHub.Domain.Exceptions;

/// <summary>
/// Ném khi dữ liệu đầu vào hoặc giá trị nghiệp vụ không thỏa mãn điều kiện hợp lệ.
/// Middleware sẽ map sang HTTP 400 Bad Request + ProblemDetails.
/// </summary>
public sealed class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        };
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
