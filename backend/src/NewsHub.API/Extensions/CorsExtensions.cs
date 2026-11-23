namespace NewsHub.API.Extensions;

/// <summary>
/// Quản lý cấu hình Cross-Origin Resource Sharing (CORS) cho ứng dụng.
/// </summary>
public static class CorsExtensions
{
    public const string FrontendCorsPolicy = "FrontendCorsPolicy";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "https://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy.SetIsOriginAllowed(origin => IsOriginAllowed(origin, allowedOrigins))
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Kiểm tra xem một origin có được phép truy cập theo chính sách CORS hay không.
    /// Cho phép các origin được cấu hình tường minh, local dev trên bất kỳ port nào,
    /// và mọi subdomain bảo mật trên nền tảng Vercel (*.vercel.app phục vụ PR Previews).
    /// </summary>
    public static bool IsOriginAllowed(string? origin, IEnumerable<string>? configuredOrigins = null)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        // 1. Chấp nhận các origin được cấu hình tường minh (appsettings.json / Render env vars)
        if (configuredOrigins != null && configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            // 2. Chấp nhận Local Development trên bất kỳ port nào
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 3. Chấp nhận Vercel Production & Dynamic PR Preview Deployments (*.vercel.app) qua HTTPS
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                (uri.Host.Equals("vercel.app", StringComparison.OrdinalIgnoreCase) ||
                 uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        return app.UseCors(FrontendCorsPolicy);
    }
}
