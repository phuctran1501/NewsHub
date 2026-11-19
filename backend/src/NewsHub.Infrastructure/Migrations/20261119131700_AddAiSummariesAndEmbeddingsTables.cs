using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace NewsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSummariesAndEmbeddingsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ai_summaries",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bullet_points = table.Column<string>(type: "jsonb", nullable: false),
                    sentiment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Neutral"),
                    extracted_tags = table.Column<string>(type: "jsonb", nullable: false),
                    ai_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "gemini-3.5-flash-lite"),
                    confidence_score = table.Column<float>(type: "real", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_summaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_summaries_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "public",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_embeddings",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_embeddings", x => x.id);
                    table.ForeignKey(
                        name: "FK_article_embeddings_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "public",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_summaries_article_id",
                schema: "public",
                table: "ai_summaries",
                column: "article_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_article_embeddings_article_id",
                schema: "public",
                table: "article_embeddings",
                column: "article_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_embeddings_cosine",
                schema: "public",
                table: "article_embeddings",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_summaries",
                schema: "public");

            migrationBuilder.DropTable(
                name: "article_embeddings",
                schema: "public");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
