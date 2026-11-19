namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Interfaces;

/// <summary>
/// Quản lý người dùng theo dõi tòa soạn.
/// Composite PK: (CompanyId, UserId)
/// </summary>
public class CompanyFollower : IHasCreatedAt
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Company? Company { get; set; }
    public User? User { get; set; }
}
