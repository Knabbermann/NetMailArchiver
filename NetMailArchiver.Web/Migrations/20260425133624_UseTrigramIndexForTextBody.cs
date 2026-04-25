using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetMailArchiver.Web.Migrations
{
    /// <inheritdoc />
    public partial class UseTrigramIndexForTextBody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_trgm extension for trigram indexes (if not already enabled)
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Drop the old Full-Text Search index
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS IX_Emails_TextBody_FTS;");

            // Create GIN Trigram index for ILIKE searches on TextBody
            // This supports case-insensitive substring searches efficiently
            migrationBuilder.Sql(@"
                CREATE INDEX IX_Emails_TextBody_Trgm 
                ON ""Emails"" 
                USING GIN (""TextBody"" gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
