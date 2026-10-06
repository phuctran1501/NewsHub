using Microsoft.EntityFrameworkCore;
using NewsHub.Domain.Entities;

namespace NewsHub.Infrastructure.Persistence;

public class NewsHubDbContext(DbContextOptions<NewsHubDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Supabase PostgreSQL mặc định sử dụng schema public
        modelBuilder.HasDefaultSchema("public");

        // Quét và áp dụng tự động toàn bộ Fluent API Configurations trong Assembly này
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NewsHubDbContext).Assembly);
    }
}
