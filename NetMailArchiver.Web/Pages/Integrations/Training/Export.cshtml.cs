using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.DataAccess;
using System.Text;
using System.Text.Json;

namespace NetMailArchiver.Web.Pages.Integrations.Training
{
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalCategorizedEmails { get; set; }
        public int TotalFeedbackEntries { get; set; }
        public int ManualCorrections { get; set; }

        public async Task OnGetAsync()
        {
            TotalCategorizedEmails = await _context.Emails.CountAsync(e => e.CategoryId != null);
            TotalFeedbackEntries = await _context.EmailCategorizationFeedbacks.CountAsync();
            ManualCorrections = await _context.EmailCategorizationFeedbacks.CountAsync(f => f.WasManuallyChanged);
        }

        public async Task<IActionResult> OnGetDownloadJsonlAsync()
        {
            var feedbacks = await _context.EmailCategorizationFeedbacks
                .Include(f => f.Email)
                .Include(f => f.FinalCategory)
                .OrderByDescending(f => f.CreatedAt)
                .Take(1000) // Limit to last 1000 for performance
                .ToListAsync();

            var jsonlBuilder = new StringBuilder();

            foreach (var feedback in feedbacks)
            {
                var trainingItem = new
                {
                    prompt = $"Subject: {feedback.Email.Subject ?? ""}\n" +
                             $"From: {feedback.Email.From ?? ""}\n" +
                             $"Body: {TruncateText(feedback.Email.TextBody ?? "", 500)}\n" +
                             $"Available categories: {string.Join(", ", await _context.Categories.Select(c => c.Name).ToListAsync())}",
                    response = feedback.FinalCategory.Name
                };

                jsonlBuilder.AppendLine(JsonSerializer.Serialize(trainingItem));
            }

            var bytes = Encoding.UTF8.GetBytes(jsonlBuilder.ToString());
            return File(bytes, "application/jsonl", $"email-training-data-{DateTime.UtcNow:yyyyMMdd}.jsonl");
        }

        public async Task<IActionResult> OnGetDownloadCsvAsync()
        {
            var feedbacks = await _context.EmailCategorizationFeedbacks
                .Include(f => f.Email)
                .Include(f => f.FinalCategory)
                .Include(f => f.AiSuggestedCategory)
                .OrderByDescending(f => f.CreatedAt)
                .Take(1000)
                .ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("EmailId,Subject,From,FinalCategory,AiSuggestedCategory,WasManuallyChanged,Confidence,CreatedAt");

            foreach (var feedback in feedbacks)
            {
                csvBuilder.AppendLine($"\"{feedback.EmailId}\"," +
                    $"\"{EscapeCsv(feedback.Email.Subject)}\"," +
                    $"\"{EscapeCsv(feedback.Email.From)}\"," +
                    $"\"{feedback.FinalCategory.Name}\"," +
                    $"\"{feedback.AiSuggestedCategory?.Name ?? ""}\"," +
                    $"{feedback.WasManuallyChanged}," +
                    $"{feedback.Confidence ?? 0}," +
                    $"\"{feedback.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            return File(bytes, "text/csv", $"email-feedback-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            return text.Substring(0, maxLength) + "...";
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
