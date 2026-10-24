using NewsHub.Application.Common.Interfaces;

namespace NewsHub.API.Services;

/// <summary>
/// Trích xuất CorrelationId từ HTTP Request Header X-Correlation-ID hoặc fallback sang TraceIdentifier.
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private const string HeaderKey = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Guid.NewGuid().ToString();
        }

        if (httpContext.Request.Headers.TryGetValue(HeaderKey, out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return httpContext.TraceIdentifier;
    }
}
