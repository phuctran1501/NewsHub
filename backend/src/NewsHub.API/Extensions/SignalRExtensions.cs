using Microsoft.AspNetCore.SignalR;
using NewsHub.API.Hubs;
using NewsHub.API.Security;
using NewsHub.API.Services;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.API.Extensions;

/// <summary>
/// Quản lý cấu hình SignalR real-time notification hub và các services liên quan.
/// </summary>
public static class SignalRExtensions
{
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        });

        // Đăng ký CustomUserIdProvider để ánh xạ định danh người dùng từ JWT claims
        services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

        // Đăng ký INotificationService cho tầng Application
        services.AddScoped<INotificationService, SignalRNotificationService>();

        return services;
    }

    public static WebApplication MapSignalRHubs(this WebApplication app)
    {
        app.MapHub<NotificationHub>("/hubs/notifications");

        return app;
    }
}
