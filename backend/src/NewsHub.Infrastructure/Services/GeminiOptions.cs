namespace NewsHub.Infrastructure.Services;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-3.5-flash-lite";
    public string EmbeddingModel { get; set; } = "text-embedding-004";
    public int TimeoutSeconds { get; set; } = 30;
}
