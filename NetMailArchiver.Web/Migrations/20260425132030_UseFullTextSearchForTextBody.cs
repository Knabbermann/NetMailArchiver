using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class UseFullTextSearchForTextBody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Emails_TextBody",
                table: "Emails");

            // Create GIN index for Full-Text Search on TextBody
            // This supports searching large text fields efficiently without size limitations
            migrationBuilder.Sql(@"
                CREATE INDEX IX_Emails_TextBody_FTS 
                ON ""Emails"" 
                USING GIN (to_tsvector('english', COALESCE(""TextBody"", '')));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Emails_TextBody",
                table: "Emails",
                column: "TextBody");
        }
    }
}
