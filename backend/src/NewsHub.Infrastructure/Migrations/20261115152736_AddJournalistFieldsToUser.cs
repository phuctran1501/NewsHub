using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalistFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "public",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                schema: "public",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "press_card_number",
                schema: "public",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bio",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone_number",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "press_card_number",
                schema: "public",
                table: "users");
        }
    }
}
