using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure.Persistence;
using NewsHub.Infrastructure.Repositories;
using NewsHub.Infrastructure.Services;
using StackExchange.Redis;

namespace NewsHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<NewsHubDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(NewsHubDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        // Redis Configuration, ConnectionMultiplexer Singleton & ICacheService
        var redisSection = configuration.GetSection(RedisOptions.SectionName);
        var redisOptions = new RedisOptions
        {
            ConnectionString = redisSection["ConnectionString"] ?? "localhost:6379,abortConnect=false",
            InstanceName = redisSection["InstanceName"] ?? "NewsHub:",
            DefaultExpirationMinutes = int.TryParse(redisSection["DefaultExpirationMinutes"], out var exp) ? exp : 30,
            ConnectTimeoutMs = int.TryParse(redisSection["ConnectTimeoutMs"], out var ct) ? ct : 5000,
            SyncTimeoutMs = int.TryParse(redisSection["SyncTimeoutMs"], out var st) ? st : 3000
        };

        services.Configure<RedisOptions>(opt =>
        {
            opt.ConnectionString = redisOptions.ConnectionString;
            opt.InstanceName = redisOptions.InstanceName;
            opt.DefaultExpirationMinutes = redisOptions.DefaultExpirationMinutes;
            opt.ConnectTimeoutMs = redisOptions.ConnectTimeoutMs;
            opt.SyncTimeoutMs = redisOptions.SyncTimeoutMs;
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configOptions = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            configOptions.AbortOnConnectFail = false; // Đảm bảo ứng dụng không bị sập nếu Redis khởi động chậm
            configOptions.ConnectTimeout = redisOptions.ConnectTimeoutMs;
            configOptions.SyncTimeout = redisOptions.SyncTimeoutMs;
            configOptions.ConnectRetry = 3;

            return ConnectionMultiplexer.Connect(configOptions);
        });

        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}
