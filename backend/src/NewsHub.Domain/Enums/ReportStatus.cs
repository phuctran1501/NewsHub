namespace NewsHub.Domain.Enums;

/// <summary>
/// Trạng thái xử lý báo cáo vi phạm bài viết của Ban biên tập / Quản trị viên.
/// </summary>
public enum ReportStatus
{
    Pending = 1,
    Resolved = 2,
    Dismissed = 3
}
