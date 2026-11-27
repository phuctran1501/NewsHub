using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace NewsHub.Infrastructure.Persistence;

public class NewsHubDbContext(DbContextOptions<NewsHubDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<ArticleReport> ArticleReports => Set<ArticleReport>();
    public DbSet<ReadingHistory> ReadingHistories => Set<ReadingHistory>();
    public DbSet<UserBookmark> UserBookmarks => Set<UserBookmark>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMember> CompanyMembers => Set<CompanyMember>();
    public DbSet<CompanyRequest> CompanyRequests => Set<CompanyRequest>();
    public DbSet<CompanyFollower> CompanyFollowers => Set<CompanyFollower>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AiSummary> AiSummaries => Set<AiSummary>();
    public DbSet<ArticleEmbedding> ArticleEmbeddings => Set<ArticleEmbedding>();

    public async Task<IReadOnlyList<SemanticSearchResultDto>> SearchArticlesByEmbeddingAsync(
        Vector embedding,
        int limit,
        double minSimilarity = 0.5,
        CancellationToken cancellationToken = default)
    {
        var maxDistance = Math.Clamp(1.0 - minSimilarity, 0.0, 2.0);

        return await ArticleEmbeddings
            .AsNoTracking()
            .Where(e => e.Article != null &&
                        e.Article.Status == ArticleStatus.Published &&
                        e.Embedding.CosineDistance(embedding) <= maxDistance)
            .OrderBy(e => e.Embedding.CosineDistance(embedding))
            .Take(limit)
            .Select(e => new SemanticSearchResultDto(
                e.ArticleId,
                e.Article!.Title,
                e.Article.Slug,
                (float)(1.0 - e.Embedding.CosineDistance(embedding)),
                e.Article.PublishedAt))
            .ToListAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Supabase PostgreSQL mặc định sử dụng schema public
        modelBuilder.HasDefaultSchema("public");

        // Kích hoạt extension pgvector cho tìm kiếm ngữ nghĩa
        modelBuilder.HasPostgresExtension("vector");

        // Quét và áp dụng tự động toàn bộ Fluent API Configurations trong Assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewsHubDbContext).Assembly);
    }
}
