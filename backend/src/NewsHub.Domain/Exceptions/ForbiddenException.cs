namespace NewsHub.Domain.Exceptions;

/// <summary>
/// Ném khi user không có quyền truy cập resource (đã xác thực nhưng không đủ quyền).
/// Middleware sẽ map sang HTTP 403 Forbidden + ProblemDetails.
/// Phân biệt với 401: 401 = chưa xác thực, 403 = đã xác thực nhưng bị cấm.
/// </summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException()
        : base("You do not have permission to perform this action.") { }

    public ForbiddenException(string message)
        : base(message) { }
}
