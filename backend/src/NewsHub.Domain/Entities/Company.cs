namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;

/// <summary>
/// Thực thể Công ty / Tòa soạn truyền thông trong nền tảng NewsHub.
/// </summary>
public class Company : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Code { get; set; }
    public required string TaxCode { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Pending;
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<CompanyMember> Members { get; set; } = new HashSet<CompanyMember>();
    public ICollection<CompanyRequest> Requests { get; set; } = new HashSet<CompanyRequest>();
    public ICollection<CompanyFollower> Followers { get; set; } = new HashSet<CompanyFollower>();
    public ICollection<Article> Articles { get; set; } = new HashSet<Article>();
}
