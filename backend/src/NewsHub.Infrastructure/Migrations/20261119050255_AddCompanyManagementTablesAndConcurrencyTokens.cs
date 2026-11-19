using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyManagementTablesAndConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "public",
                table: "articles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    tax_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.ForeignKey(
                        name: "FK_companies_users_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_followers",
                schema: "public",
                columns: table => new
                {
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_followers", x => new { x.company_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_company_followers_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_followers_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_members",
                schema: "public",
                columns: table => new
                {
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Member"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_members", x => new { x.company_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_company_members_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_requests",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    journalist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_requests_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "public",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_requests_users_journalist_id",
                        column: x => x.journalist_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_code",
                schema: "public",
                table: "companies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_owner_id",
                schema: "public",
                table: "companies",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_companies_status",
                schema: "public",
                table: "companies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_companies_tax_code",
                schema: "public",
                table: "companies",
                column: "tax_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_followers_user_id",
                schema: "public",
                table: "company_followers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_company_members_company_id",
                schema: "public",
                table: "company_members",
                column: "company_id",
                unique: true,
                filter: "role = 'Owner'");

            migrationBuilder.CreateIndex(
                name: "IX_company_members_user_id",
                schema: "public",
                table: "company_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_company_requests_company_id_journalist_id_type",
                schema: "public",
                table: "company_requests",
                columns: new[] { "company_id", "journalist_id", "type" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_company_requests_company_id_status",
                schema: "public",
                table: "company_requests",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_company_requests_journalist_id_status",
                schema: "public",
                table: "company_requests",
                columns: new[] { "journalist_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "FK_articles_companies_company_id",
                schema: "public",
                table: "articles",
                column: "company_id",
                principalSchema: "public",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articles_companies_company_id",
                schema: "public",
                table: "articles");

            migrationBuilder.DropTable(
                name: "company_followers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "company_members",
                schema: "public");

            migrationBuilder.DropTable(
                name: "company_requests",
                schema: "public");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "public",
                table: "articles");
        }
    }
}
