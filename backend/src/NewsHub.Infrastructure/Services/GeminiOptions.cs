namespace NewsHub.Infrastructure.Services;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v1beta";
    public string ModelName { get; set; } = "gemini-3.5-flash-lite";
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public int TimeoutSeconds { get; set; } = 30;
}
