namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;

public class AiSummaryConfiguration : IEntityTypeConfiguration<AiSummary>
{
    public void Configure(EntityTypeBuilder<AiSummary> builder)
    {
        builder.ToTable("ai_summaries");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.ArticleId)
            .HasColumnName("article_id")
            .IsRequired();

        builder.HasIndex(s => s.ArticleId)
            .IsUnique();

        builder.Property(s => s.BulletPoints)
            .HasColumnName("bullet_points")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.Sentiment)
            .HasColumnName("sentiment")
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(ArticleSentiment.Neutral)
            .HasSentinel((ArticleSentiment)0)
            .IsRequired();

        builder.Property(s => s.ExtractedTags)
            .HasColumnName("extracted_tags")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(s => s.AiModel)
            .HasColumnName("ai_model")
            .HasMaxLength(50)
            .HasDefaultValue("gemini-3.5-flash-lite")
            .IsRequired();

        builder.Property(s => s.ConfidenceScore)
            .HasColumnName("confidence_score")
            .IsRequired();

        builder.Property(s => s.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();

        // 1:1 relationship with Article
        builder.HasOne(s => s.Article)
            .WithOne(a => a.AiSummary)
            .HasForeignKey<AiSummary>(s => s.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
