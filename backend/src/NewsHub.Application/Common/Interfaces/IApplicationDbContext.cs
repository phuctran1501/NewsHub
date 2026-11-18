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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
