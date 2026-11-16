using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "public",
                table: "refresh_tokens",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "public",
                table: "refresh_tokens");
        }
    }
}
