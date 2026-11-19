namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class ArticleEmbeddingConfiguration : IEntityTypeConfiguration<ArticleEmbedding>
{
    public void Configure(EntityTypeBuilder<ArticleEmbedding> builder)
    {
        builder.ToTable("article_embeddings");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.ArticleId)
            .HasColumnName("article_id")
            .IsRequired();

        builder.HasIndex(e => e.ArticleId)
            .IsUnique();

        builder.Property(e => e.Embedding)
            .HasColumnName("embedding")
            .HasColumnType("vector(768)")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Index tìm kiếm vector embedding theo khoảng cách Cosine (pgvector HNSW index)
        builder.HasIndex(e => e.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops")
            .HasDatabaseName("ix_article_embeddings_cosine");

        // 1:1 relationship with Article
        builder.HasOne(e => e.Article)
            .WithOne(a => a.ArticleEmbedding)
            .HasForeignKey<ArticleEmbedding>(e => e.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
