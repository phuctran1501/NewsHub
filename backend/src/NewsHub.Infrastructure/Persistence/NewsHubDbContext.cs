using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Entities;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Supabase PostgreSQL mặc định sử dụng schema public
        modelBuilder.HasDefaultSchema("public");

        // Quét và áp dụng tự động toàn bộ Fluent API Configurations trong Assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewsHubDbContext).Assembly);
    }
}
