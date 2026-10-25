using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace NewsHub.API.Security;

/// <summary>
/// Bộ lọc ủy quyền truy cập Hangfire Dashboard qua giao thức HTTP Basic Authentication.
/// Bảo vệ dashboard an toàn trên mọi môi trường (Local, Staging, Production).
/// </summary>
public class HangfireDashboardAuthorizationFilter(IConfiguration configuration) : IDashboardAuthorizationFilter
{
    private readonly string _configuredUsername = configuration["Hangfire:DashboardUsername"] ?? "admin";
    private readonly string _configuredPassword = configuration["Hangfire:DashboardPassword"] ?? "";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Kiểm tra header Authorization
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var headerValue) ||
            !string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(headerValue.Parameter))
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        try
        {
            var rawCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter));
            var parts = rawCredentials.Split(':', 2);
            if (parts.Length != 2)
            {
                SetChallengeResponse(httpContext);
                return false;
            }

            var username = parts[0];
            var password = parts[1];

            // So sánh an toàn thời gian cố định (Fixed-time comparison) để chống timing attack
            var usernameMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(username),
                Encoding.UTF8.GetBytes(_configuredUsername));

            var passwordMatch = !string.IsNullOrEmpty(_configuredPassword) &&
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(password),
                    Encoding.UTF8.GetBytes(_configuredPassword));

            if (usernameMatch && passwordMatch)
            {
                return true;
            }
        }
        catch
        {
            // Bỏ qua lỗi decode base64 và gửi phản hồi 401
        }

        SetChallengeResponse(httpContext);
        return false;
    }

    private static void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
    }
}
