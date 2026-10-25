using Hangfire;
using Hangfire.PostgreSql;
using NewsHub.API.Security;

namespace NewsHub.API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

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
}
