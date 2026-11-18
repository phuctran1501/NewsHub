using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToReadingHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_reading_histories_user_id_article_id",
                schema: "public",
                table: "reading_histories",
                columns: new[] { "user_id", "article_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reading_histories_user_id_article_id",
                schema: "public",
                table: "reading_histories");
        }
    }
}
