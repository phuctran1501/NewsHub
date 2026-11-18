namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;

public class ArticleReportConfiguration : IEntityTypeConfiguration<ArticleReport>
{
    public void Configure(EntityTypeBuilder<ArticleReport> builder)
    {
        builder.ToTable("article_reports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.ArticleId)
            .HasColumnName("article_id")
            .IsRequired();

        builder.Property(r => r.ReporterId)
            .HasColumnName("reporter_id");

        builder.Property(r => r.Reason)
            .HasColumnName("reason")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Details)
            .HasColumnName("details")
            .HasColumnType("text");

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .HasDefaultValue(ReportStatus.Pending)
            .HasSentinel((ReportStatus)0)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(r => r.ResolvedBy)
            .HasColumnName("resolved_by");

        // Indexes
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
        builder.HasIndex(r => r.ArticleId);

        // Relationships
        builder.HasOne(r => r.Article)
            .WithMany(a => a.Reports)
            .HasForeignKey(r => r.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reporter)
            .WithMany(u => u.ReportedArticles)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Resolver)
            .WithMany()
            .HasForeignKey(r => r.ResolvedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
