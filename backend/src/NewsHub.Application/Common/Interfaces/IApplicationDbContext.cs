using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Entities;
using Pgvector;

namespace NewsHub.Application.Common.Interfaces;

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
    DbSet<AiSummary> AiSummaries { get; }
    DbSet<ArticleEmbedding> ArticleEmbeddings { get; }

    Task<IReadOnlyList<SemanticSearchResultDto>> SearchArticlesByEmbeddingAsync(
        Vector embedding,
        int limit,
        double minSimilarity = 0.65,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
