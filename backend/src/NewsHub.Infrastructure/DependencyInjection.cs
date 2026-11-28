using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure.Jobs;
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
        var rawConnection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        var connectionString = rawConnection.Trim().Trim('"', '\'');

        services.AddDbContext<NewsHubDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
                npgsqlOptions.MigrationsAssembly(typeof(NewsHubDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NewsHubDbContext>());

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

        // Cloudinary Configuration, Singleton ICloudinary / Cloudinary & ICloudinaryService
        var cloudinarySection = configuration.GetSection(CloudinaryOptions.SectionName);
        services.Configure<CloudinaryOptions>(opt =>
        {
            opt.CloudName = cloudinarySection["CloudName"] ?? string.Empty;
            opt.ApiKey = cloudinarySection["ApiKey"] ?? string.Empty;
            opt.ApiSecret = cloudinarySection["ApiSecret"] ?? string.Empty;
        });

        services.AddSingleton<Cloudinary>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CloudinaryOptions>>().Value;
            var account = new Account(options.CloudName, options.ApiKey, options.ApiSecret);
            return new Cloudinary(account);
        });

        services.AddSingleton<ICloudinary>(sp => sp.GetRequiredService<Cloudinary>());
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        // JWT & Password Hasher
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(opt =>
        {
            opt.SecretKey = jwtSection["SecretKey"] ?? string.Empty;
            opt.Issuer = jwtSection["Issuer"] ?? "NewsHubAPI";
            opt.Audience = jwtSection["Audience"] ?? "NewsHubClients";
            opt.AccessTokenExpirationMinutes = int.TryParse(jwtSection["AccessTokenExpirationMinutes"], out var at) ? at : 15;
            opt.RefreshTokenExpirationDays = int.TryParse(jwtSection["RefreshTokenExpirationDays"], out var rt) ? rt : 7;
        });

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizerService>();
        services.AddSingleton<IDraftPreviewTokenService, DraftPreviewTokenService>();
        services.AddScoped<SyncArticleViewsJob>();

        // Google Gemini AI Configuration & Services
        var geminiSection = configuration.GetSection(GeminiOptions.SectionName);
        services.Configure<GeminiOptions>(opt =>
        {
            opt.ApiKey = geminiSection["ApiKey"] ?? string.Empty;
            opt.ModelName = geminiSection["ModelName"] ?? "gemini-3.5-flash-lite";
            opt.EmbeddingModel = geminiSection["EmbeddingModel"] ?? "text-embedding-004";
            opt.TimeoutSeconds = int.TryParse(geminiSection["TimeoutSeconds"], out var to) ? to : 30;
        });

        services.AddMemoryCache();
        services.AddHttpClient<IMarketTickerService, MarketTickerService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NewsHub/1.0 (+https://newshub.vn)");
        });

        services.AddHttpClient<IAiService, GeminiAiService>();
        services.AddScoped<IAiBackgroundJobs, AiBackgroundJobs>();

        return services;
    }
}
