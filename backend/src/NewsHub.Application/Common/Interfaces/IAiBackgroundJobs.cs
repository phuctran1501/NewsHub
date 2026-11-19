namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Abstraction các tác vụ nền AI xử lý qua Hangfire.
/// </summary>
public interface IAiBackgroundJobs
{
    /// <summary>
    /// Xử lý tạo tóm tắt thông minh, phân tích cảm xúc và trích xuất từ khóa cho bài viết.
    /// </summary>
    Task GenerateArticleSummaryAsync(Guid articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sinh vector nhúng 768 chiều cho bài viết phục vụ tìm kiếm ngữ nghĩa.
    /// </summary>
    Task GenerateArticleEmbeddingAsync(Guid articleId, CancellationToken cancellationToken = default);
}
