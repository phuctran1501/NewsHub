namespace NewsHub.Application.Common.Interfaces;

using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Enums;

/// <summary>
/// Abstraction kết nối với dịch vụ Trí tuệ Nhân tạo Google Gemini (Flash & Text Embedding).
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Gợi ý tối đa 3 chuyên mục phù hợp từ danh sách có sẵn dựa trên tiêu đề và đoạn trích bài viết.
    /// </summary>
    Task<IReadOnlyList<CategorySuggestionDto>> SuggestCategoriesAsync(
        string title,
        string contentSnippet,
        IReadOnlyList<CategoryCandidateDto> availableCategories,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo bản tóm tắt ý chính (bullet points), phân tích sắc thái cảm xúc và trích xuất từ khóa cho bài viết.
    /// </summary>
    Task<AiSummaryResultDto> GenerateSummaryAndSentimentAsync(
        string title,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh vector nhúng 768 chiều (mặc định tối ưu cho truy vấn tìm kiếm RETRIEVAL_QUERY).
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh vector nhúng 768 chiều chuẩn hóa L2 kèm taskType bất đối xứng (RETRIEVAL_DOCUMENT / RETRIEVAL_QUERY).
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        EmbeddingTaskType taskType,
        string? title = null,
        CancellationToken cancellationToken = default);
}
