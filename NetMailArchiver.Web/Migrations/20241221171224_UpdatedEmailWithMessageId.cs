using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEmailWithMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageId",
                table: "Emails",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Emails");
        }
    }
}
