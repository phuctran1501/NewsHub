namespace NewsHub.Domain.Enums;

/// <summary>
/// Trạng thái vòng đời của bài viết trong tòa soạn điện tử.
/// </summary>
public enum ArticleStatus
{
    Draft = 1,
    PendingCompanyReview = 2,
    Published = 3,
    Archived = 4,
    Rejected = 5
}
