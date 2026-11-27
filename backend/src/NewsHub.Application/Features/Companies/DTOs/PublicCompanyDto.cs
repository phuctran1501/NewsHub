namespace NewsHub.Application.Features.Companies.DTOs;

/// <summary>
/// DTO công khai của Tòa soạn / Công ty truyền thông dành cho Độc giả (tuyệt đối không lộ Mã số thuế).
/// </summary>
public sealed record PublicCompanyDto(
    Guid Id,
    string Name,
    string Code,
    string? OwnerName,
    int MemberCount,
    int PublishedArticleCount,
    int FollowerCount,
    DateTimeOffset CreatedAt);
