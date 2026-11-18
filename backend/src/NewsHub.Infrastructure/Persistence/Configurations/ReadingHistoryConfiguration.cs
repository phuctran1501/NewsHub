namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class ReadingHistoryConfiguration : IEntityTypeConfiguration<ReadingHistory>
{
    public void Configure(EntityTypeBuilder<ReadingHistory> builder)
    {
        builder.ToTable("reading_histories");

        builder.HasKey(rh => rh.Id);
        builder.Property(rh => rh.Id)
            .HasColumnName("id");

        builder.Property(rh => rh.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rh => rh.ArticleId)
            .HasColumnName("article_id")
            .IsRequired();

        builder.Property(rh => rh.ReadAt)
            .HasColumnName("read_at")
            .IsRequired();

        // Index
        builder.HasIndex(rh => new { rh.UserId, rh.ArticleId })
            .IsUnique();
        builder.HasIndex(rh => new { rh.UserId, rh.ReadAt });
        builder.HasIndex(rh => rh.ArticleId);

        // Relationships
        builder.HasOne(rh => rh.User)
            .WithMany(u => u.ReadingHistories)
            .HasForeignKey(rh => rh.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rh => rh.Article)
            .WithMany(a => a.ReadingHistories)
            .HasForeignKey(rh => rh.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
