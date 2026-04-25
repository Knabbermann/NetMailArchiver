using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTextBodyAndSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextBody",
                table: "Emails",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Date",
                table: "Emails",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_From",
                table: "Emails",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_Subject",
                table: "Emails",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_Emails_TextBody",
                table: "Emails",
                column: "TextBody");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Emails_Date",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_From",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_Subject",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_TextBody",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "TextBody",
                table: "Emails");
        }
    }
}
