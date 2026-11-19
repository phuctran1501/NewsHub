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
    public string? PhoneNumber { get; set; }
    public string? PressCardNumber { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();
    public ICollection<MediaFile> MediaFiles { get; set; } = new HashSet<MediaFile>();
    public ICollection<Article> Articles { get; set; } = new HashSet<Article>();
    public ICollection<ReadingHistory> ReadingHistories { get; set; } = new HashSet<ReadingHistory>();
    public ICollection<ArticleReport> ReportedArticles { get; set; } = new HashSet<ArticleReport>();
    public ICollection<UserBookmark> Bookmarks { get; set; } = new HashSet<UserBookmark>();
    public ICollection<Company> OwnedCompanies { get; set; } = new HashSet<Company>();
    public ICollection<CompanyMember> CompanyMemberships { get; set; } = new HashSet<CompanyMember>();
    public ICollection<CompanyRequest> CompanyRequests { get; set; } = new HashSet<CompanyRequest>();
    public ICollection<CompanyFollower> FollowedCompanies { get; set; } = new HashSet<CompanyFollower>();
    public ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
}