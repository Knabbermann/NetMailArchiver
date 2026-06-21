using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndEmailCategoryRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Emails",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Emails_CategoryId",
                table: "Emails",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            // Seed default categories
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Name", "Description", "Color", "Icon", "IsSystem", "IsDefault", "CreatedAt", "EmailCount" },
                values: new object[,]
                {
                    { "Uncategorized", "Emails without a category", "#6c757d", "fa-question-circle", true, true, DateTime.UtcNow, 0 },
                    { "Invoice", "Bills and invoices", "#dc3545", "fa-file-invoice-dollar", true, false, DateTime.UtcNow, 0 },
                    { "Support", "Customer support emails", "#0dcaf0", "fa-headset", true, false, DateTime.UtcNow, 0 },
                    { "Newsletter", "Marketing and newsletters", "#ffc107", "fa-newspaper", true, false, DateTime.UtcNow, 0 },
                    { "Important", "High priority emails", "#d63384", "fa-exclamation-circle", true, false, DateTime.UtcNow, 0 },
                    { "Personal", "Personal correspondence", "#20c997", "fa-user", true, false, DateTime.UtcNow, 0 },
                    { "Work", "Work-related emails", "#0d6efd", "fa-briefcase", true, false, DateTime.UtcNow, 0 },
                    { "Spam", "Spam and junk emails", "#495057", "fa-trash", true, false, DateTime.UtcNow, 0 }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_Categories_CategoryId",
                table: "Emails",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Emails_Categories_CategoryId",
                table: "Emails");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Emails_CategoryId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Emails");
        }
    }
}
