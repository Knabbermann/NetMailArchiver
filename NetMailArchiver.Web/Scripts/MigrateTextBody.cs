using Microsoft.EntityFrameworkCore;
using NetMailArchiver.DataAccess;
using System.Text.RegularExpressions;

namespace NetMailArchiver.Web.Scripts
{
    /// <summary>
    /// One-time migration script to populate TextBody for existing emails
    /// Run this after applying the AddTextBodyAndSearchIndexes migration
    /// </summary>
    public class MigrateTextBody
    {
        public static async Task RunAsync(ApplicationDbContext context)
        {
            Console.WriteLine("Starting TextBody migration for existing emails...");

            var batchSize = 100;
            var totalProcessed = 0;

            var emailsToProcess = await context.Emails
                .Where(e => e.TextBody == null || e.TextBody == "")
                .Select(e => new { e.Id, e.HtmlBody })
                .ToListAsync();

            var totalEmails = emailsToProcess.Count;
            Console.WriteLine($"Found {totalEmails} emails to process.");

            foreach (var batch in emailsToProcess.Chunk(batchSize))
            {
                foreach (var emailData in batch)
                {
                    var email = await context.Emails.FindAsync(emailData.Id);
                    if (email != null)
                    {
                        email.TextBody = CleanHtmlToText(emailData.HtmlBody);
                    }
                }

                await context.SaveChangesAsync();
                totalProcessed += batch.Length;

                var progress = (totalProcessed * 100) / totalEmails;
                Console.WriteLine($"Progress: {totalProcessed}/{totalEmails} ({progress}%)");
            }

            Console.WriteLine("TextBody migration completed!");
        }

        private static string CleanHtmlToText(string? htmlBody)
        {
            if (string.IsNullOrWhiteSpace(htmlBody))
                return string.Empty;

            var text = htmlBody;

            // Remove script tags and their content
            text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);

            // Remove style tags and their content
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

            // Remove head tag and its content
            text = Regex.Replace(text, @"<head[^>]*>[\s\S]*?</head>", string.Empty, RegexOptions.IgnoreCase);

            // Remove HTML comments
            text = Regex.Replace(text, @"<!--[\s\S]*?-->", string.Empty);

            // Remove all HTML tags
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Remove extra whitespace and newlines
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }
    }
}
