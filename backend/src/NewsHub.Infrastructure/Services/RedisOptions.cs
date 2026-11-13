namespace NewsHub.Infrastructure.Services;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Connection string tới Redis Server (ví dụ: localhost:6379,abortConnect=false).
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";

    /// <summary>
    /// Tiền tố phân vùng cho toàn bộ key của hệ thống (ví dụ: NewsHub:).
    /// </summary>
    public string InstanceName { get; set; } = "NewsHub:";

    /// <summary>
    /// Thời gian sống mặc định của cache nếu không chỉ định (phút).
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Timeout kết nối Redis (mili-giây).
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Timeout đồng bộ lệnh Redis (mili-giây).
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 3000;
}

