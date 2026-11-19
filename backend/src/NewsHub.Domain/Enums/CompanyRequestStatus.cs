namespace NewsHub.Domain.Enums;

/// <summary>
/// Trạng thái xử lý của yêu cầu gia nhập hoặc lời mời tòa soạn.
/// </summary>
public enum CompanyRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Canceled = 4
}
