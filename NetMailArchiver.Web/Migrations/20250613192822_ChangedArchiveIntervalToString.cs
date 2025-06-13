using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class ChangedArchiveIntervalToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ArchiveInterval",
                table: "ImapInformations",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ArchiveInterval",
                table: "ImapInformations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
