namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class UserBookmarkConfiguration : IEntityTypeConfiguration<UserBookmark>
{
    public void Configure(EntityTypeBuilder<UserBookmark> builder)
    {
        builder.ToTable("user_bookmarks");

        // Composite Primary Key (user_id, article_id)
        builder.HasKey(ub => new { ub.UserId, ub.ArticleId });

        builder.Property(ub => ub.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ub => ub.ArticleId)
            .HasColumnName("article_id")
            .IsRequired();

        builder.Property(ub => ub.SavedAt)
            .HasColumnName("saved_at")
            .IsRequired();

        // Index lấy danh sách bookmark của user sắp xếp theo thời gian lưu mới nhất
        builder.HasIndex(ub => new { ub.UserId, ub.SavedAt })
            .HasDatabaseName("ix_user_bookmarks_user_saved");

        builder.HasIndex(ub => ub.ArticleId);

        // Relationships
        builder.HasOne(ub => ub.User)
            .WithMany(u => u.Bookmarks)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.Article)
            .WithMany(a => a.Bookmarks)
            .HasForeignKey(ub => ub.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
