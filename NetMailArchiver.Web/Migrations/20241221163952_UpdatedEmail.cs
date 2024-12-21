using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImapInformationId",
                table: "Emails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Emails_ImapInformationId",
                table: "Emails",
                column: "ImapInformationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Emails_ImapInformations_ImapInformationId",
                table: "Emails",
                column: "ImapInformationId",
                principalTable: "ImapInformations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Emails_ImapInformations_ImapInformationId",
                table: "Emails");

            migrationBuilder.DropIndex(
                name: "IX_Emails_ImapInformationId",
                table: "Emails");

            migrationBuilder.DropColumn(
                name: "ImapInformationId",
                table: "Emails");
        }
    }
}
