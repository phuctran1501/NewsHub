namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// Kết quả sinh mã token xem thử bài viết nháp (hiệu lực 2 giờ).
/// </summary>
public sealed record DraftPreviewTokenDto(
    string Token,
    string PreviewUrl,
    DateTimeOffset ExpiresAt);
