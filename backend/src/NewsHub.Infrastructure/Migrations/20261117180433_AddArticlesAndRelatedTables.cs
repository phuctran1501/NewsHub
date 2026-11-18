using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticlesAndRelatedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    slug = table.Column<string>(type: "character varying(350)", maxLength: 350, nullable: false),
                    content_html = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reading_time_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    view_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.id);
                    table.ForeignKey(
                        name: "FK_articles_media_files_cover_image_id",
                        column: x => x.cover_image_id,
                        principalSchema: "public",
                        principalTable: "media_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_articles_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_categories",
                schema: "public",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_ai_assigned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_categories", x => new { x.article_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_article_categories_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "public",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_categories_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "public",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_reports",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_article_reports_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "public",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_reports_users_reporter_id",
                        column: x => x.reporter_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_article_reports_users_resolved_by",
                        column: x => x.resolved_by,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reading_histories",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reading_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_reading_histories_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "public",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reading_histories_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_categories_category_id",
                schema: "public",
                table: "article_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_article_reports_article_id",
                schema: "public",
                table: "article_reports",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "IX_article_reports_reporter_id",
                schema: "public",
                table: "article_reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "IX_article_reports_resolved_by",
                schema: "public",
                table: "article_reports",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "IX_article_reports_status_created_at",
                schema: "public",
                table: "article_reports",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_articles_author_id",
                schema: "public",
                table: "articles",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_company_id",
                schema: "public",
                table: "articles",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_cover_image_id",
                schema: "public",
                table: "articles",
                column: "cover_image_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_published_at",
                schema: "public",
                table: "articles",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "IX_articles_slug",
                schema: "public",
                table: "articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_articles_view_count",
                schema: "public",
                table: "articles",
                column: "view_count");

            migrationBuilder.CreateIndex(
                name: "IX_reading_histories_article_id",
                schema: "public",
                table: "reading_histories",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "IX_reading_histories_user_id_read_at",
                schema: "public",
                table: "reading_histories",
                columns: new[] { "user_id", "read_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_categories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "article_reports",
                schema: "public");

            migrationBuilder.DropTable(
                name: "reading_histories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "articles",
                schema: "public");
        }
    }
}
