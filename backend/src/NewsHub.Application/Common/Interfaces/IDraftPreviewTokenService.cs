namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Abstraction sinh và giải mã Preview Token JWT ngắn hạn (2 giờ) xem trước bài viết nháp.
/// </summary>
public interface IDraftPreviewTokenService
{
    /// <summary>
    /// Sinh chuỗi JWT token chứa claim article_id và thời hạn hiệu lực.
    /// </summary>
    string GeneratePreviewToken(Guid articleId, TimeSpan? lifetime = null);

    /// <summary>
    /// Xác thực token và lấy ra articleId tương ứng nếu hợp lệ.
    /// </summary>
    bool ValidatePreviewToken(string token, out Guid articleId);
}
