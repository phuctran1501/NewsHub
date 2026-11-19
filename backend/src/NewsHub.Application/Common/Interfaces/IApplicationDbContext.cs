namespace NewsHub.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using NewsHub.Domain.Entities;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<MediaFile> MediaFiles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Article> Articles { get; }
    DbSet<ArticleCategory> ArticleCategories { get; }
    DbSet<ArticleReport> ArticleReports { get; }
    DbSet<ReadingHistory> ReadingHistories { get; }
    DbSet<UserBookmark> UserBookmarks { get; }
    DbSet<Company> Companies { get; }
    DbSet<CompanyMember> CompanyMembers { get; }
    DbSet<CompanyRequest> CompanyRequests { get; }
    DbSet<CompanyFollower> CompanyFollowers { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
