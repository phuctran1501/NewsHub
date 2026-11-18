namespace NewsHub.Domain.Enums;

/// <summary>
/// Lý do độc giả báo cáo vi phạm bài viết.
/// </summary>
public enum ReportReason
{
    Spam = 1,
    Misinformation = 2,
    Harassment = 3,
    CopyrightViolation = 4,
    Other = 5
}
