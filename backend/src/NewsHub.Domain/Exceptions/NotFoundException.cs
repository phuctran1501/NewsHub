namespace NewsHub.Domain.Exceptions;

/// <summary>
/// Ném khi một resource được yêu cầu không tồn tại trong hệ thống.
/// Middleware sẽ map sang HTTP 404 Not Found + ProblemDetails.
/// </summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.") { }

    public NotFoundException(string message)
        : base(message) { }
}
