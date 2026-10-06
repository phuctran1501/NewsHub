using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;
using NewsHub.Domain.ValueObjects;

namespace NewsHub.Domain.Entities;

public class User : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required Email Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();
}