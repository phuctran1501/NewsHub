namespace NewsHub.Domain.Exceptions;

/// <summary>
/// Base class cho tất cả domain exceptions của NewsHub.
/// Cho phép middleware bắt theo loại một cách có chọn lọc.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
