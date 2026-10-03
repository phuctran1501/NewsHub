namespace NewsHub.Domain.Exceptions;

/// <summary>
/// Ném khi tạo resource bị trùng lặp (vd: email đã tồn tại, slug đã dùng).
/// Middleware sẽ map sang HTTP 409 Conflict + ProblemDetails.
/// </summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string resourceName, string fieldName, object value)
        : base($"{resourceName} with {fieldName} '{value}' already exists.") { }

    public ConflictException(string message)
        : base(message) { }
}
