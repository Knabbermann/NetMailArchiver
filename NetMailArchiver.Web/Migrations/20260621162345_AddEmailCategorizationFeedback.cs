using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailCategorizationFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailCategorizationFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiSuggestedCategoryId = table.Column<int>(type: "integer", nullable: true),
                    FinalCategoryId = table.Column<int>(type: "integer", nullable: false),
                    WasManuallyChanged = table.Column<bool>(type: "boolean", nullable: false),
                    EmailFrom = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmailSubject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailCategorizationFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailCategorizationFeedbacks_Categories_AiSuggestedCategory~",
                        column: x => x.AiSuggestedCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailCategorizationFeedbacks_Categories_FinalCategoryId",
                        column: x => x.FinalCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailCategorizationFeedbacks_Emails_EmailId",
                        column: x => x.EmailId,
                        principalTable: "Emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCategorizationFeedbacks_AiSuggestedCategoryId",
                table: "EmailCategorizationFeedbacks",
                column: "AiSuggestedCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCategorizationFeedbacks_CreatedAt",
                table: "EmailCategorizationFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCategorizationFeedbacks_EmailFrom",
                table: "EmailCategorizationFeedbacks",
                column: "EmailFrom");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCategorizationFeedbacks_EmailId",
                table: "EmailCategorizationFeedbacks",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCategorizationFeedbacks_EmailSubject",
                table: "EmailCategorizationFeedbacks",
                column: "EmailSubject");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCategorizationFeedbacks_FinalCategoryId",
                table: "EmailCategorizationFeedbacks",
                column: "FinalCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailCategorizationFeedbacks");
        }
    }
}
