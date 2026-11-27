namespace NewsHub.Domain.Exceptions;

using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;

/// <summary>
/// Ngoại lệ ném ra khi dịch vụ phụ thuộc bên ngoài (AI, Redis, Storage...) tạm thời không khả dụng.
/// Middleware sẽ tự động ánh xạ sang HTTP 503 Service Unavailable.
/// </summary>
public class ServiceUnavailableException : AppException
{
    public ServiceUnavailableException(string message)
        : base(message, AppErrorKind.ServiceUnavailable, ErrorCodes.AiServiceUnavailable)
    {
    }

    public ServiceUnavailableException(string message, string errorCode)
        : base(message, AppErrorKind.ServiceUnavailable, errorCode)
    {
    }
}
