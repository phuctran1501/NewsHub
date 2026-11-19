namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;

/// <summary>
/// Thành viên / Nhà báo thuộc tổ chức tòa soạn.
/// Composite PK: (CompanyId, UserId)
/// </summary>
public class CompanyMember
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public CompanyMemberRole Role { get; set; } = CompanyMemberRole.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Company? Company { get; set; }
    public User? User { get; set; }
}
