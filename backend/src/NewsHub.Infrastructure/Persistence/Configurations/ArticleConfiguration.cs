namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.AuthorId)
            .HasColumnName("author_id")
            .IsRequired();

        builder.Property(a => a.CompanyId)
            .HasColumnName("company_id");

        builder.Property(a => a.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(a => a.Slug)
            .HasColumnName("slug")
            .HasMaxLength(350)
            .IsRequired();

        builder.HasIndex(a => a.Slug)
            .IsUnique();

        builder.Property(a => a.ContentHtml)
            .HasColumnName("content_html")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.SourceName)
            .HasColumnName("source_name")
            .HasMaxLength(100);

        builder.Property(a => a.SourceUrl)
            .HasColumnName("source_url")
            .HasMaxLength(500);

        builder.Property(a => a.CoverImageId)
            .HasColumnName("cover_image_id");

        builder.Property(a => a.ReadingTimeMinutes)
            .HasColumnName("reading_time_minutes")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(a => a.ViewCount)
            .HasColumnName("view_count")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(a => a.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(a => a.AuthorId);
        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.PublishedAt);
        builder.HasIndex(a => a.ViewCount);

        // Foreign keys & Relationships
        builder.HasOne(a => a.Author)
            .WithMany(u => u.Articles)
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CoverImage)
            .WithMany(m => m.CoverArticles)
            .HasForeignKey(a => a.CoverImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Company)
            .WithMany(c => c.Articles)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
