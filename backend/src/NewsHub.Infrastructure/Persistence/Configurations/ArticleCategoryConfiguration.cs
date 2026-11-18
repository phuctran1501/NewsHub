namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class ArticleCategoryConfiguration : IEntityTypeConfiguration<ArticleCategory>
{
    public void Configure(EntityTypeBuilder<ArticleCategory> builder)
    {
        builder.ToTable("article_categories");

        builder.HasKey(ac => new { ac.ArticleId, ac.CategoryId });

        builder.Property(ac => ac.ArticleId)
            .HasColumnName("article_id");

        builder.Property(ac => ac.CategoryId)
            .HasColumnName("category_id");

        builder.Property(ac => ac.IsAiAssigned)
            .HasColumnName("is_ai_assigned")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne(ac => ac.Article)
            .WithMany(a => a.ArticleCategories)
            .HasForeignKey(ac => ac.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ac => ac.Category)
            .WithMany(c => c.ArticleCategories)
            .HasForeignKey(ac => ac.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
