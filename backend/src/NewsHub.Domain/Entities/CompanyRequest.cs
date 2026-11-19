namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;

/// <summary>
/// Lời mời hoặc yêu cầu xin gia nhập tòa soạn.
/// </summary>
public class CompanyRequest : IHasCreatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid CompanyId { get; set; }
    public Guid JournalistId { get; set; }
    public CompanyRequestType Type { get; set; }
    public CompanyRequestStatus Status { get; set; } = CompanyRequestStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }

    // Navigation properties
    public Company? Company { get; set; }
    public User? Journalist { get; set; }
}
