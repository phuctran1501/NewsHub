using Hangfire;
using Hangfire.PostgreSql;
using NewsHub.API.Security;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure.Jobs;

namespace NewsHub.API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Ưu tiên chuỗi kết nối riêng cho Hangfire (Direct Port 5432 Session Mode trên Supabase)
        // Nếu không có, fallback sang DefaultConnection (tương thích 100% Local Development)
        var rawConnection = configuration.GetConnectionString("HangfireConnection")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Neither 'HangfireConnection' nor 'DefaultConnection' was found.");

        // Defensive trim: loại bỏ khoảng trắng và dấu nháy kép/đơn thừa nếu môi trường (Docker --env-file) giữ nguyên
        var connectionString = rawConnection.Trim().Trim('"', '\'');

        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings()
               .UsePostgreSqlStorage(options =>
               {
                   options.UseNpgsqlConnection(connectionString);
               }, new PostgreSqlStorageOptions
               {
                   SchemaName = "hangfire",
                   PrepareSchemaIfNecessary = true,
                   QueuePollInterval = TimeSpan.FromSeconds(15)
               });
        });

        // Khởi chạy Hangfire Server worker pool
        services.AddHangfireServer(options =>
        {
            options.ServerName = $"NewsHub-Server-{Environment.MachineName}";
            options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 4);
            options.Queues = ["critical", "default", "ai-summarize"];
        });

        return services;
    }

    public static WebApplication UseHangfireDashboardCustom(
        this WebApplication app,
        IConfiguration configuration)
    {
        var dashboardPath = configuration["Hangfire:DashboardPath"] ?? "/hangfire";

        app.UseHangfireDashboard(dashboardPath, new DashboardOptions
        {
            DashboardTitle = "NewsHub Task Queue Dashboard",
            Authorization = [new HangfireDashboardAuthorizationFilter(configuration)],
            DarkModeEnabled = true
        });

        return app;
    }

    public static WebApplication UseHangfireRecurringJobs(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var backgroundJobService = scope.ServiceProvider.GetService<IBackgroundJobService>();
        backgroundJobService?.AddOrUpdateRecurringJob<SyncArticleViewsJob>(
            "sync-article-views",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *");

        return app;
    }
}
