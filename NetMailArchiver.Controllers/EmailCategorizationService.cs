using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace NetMailArchiver.Services
{
    public interface IEmailCategorizationService
    {
        Task<bool> CategorizeEmailAsync(Guid emailId);
        Task<CategorizationResult> CategorizeAllEmailsAsync(IProgress<CategorizationProgress>? progress = null, CancellationToken cancellationToken = default);
        Task<Category?> GetCategoryFromAIAsync(Email email);
    }

    public class EmailCategorizationService : IEmailCategorizationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmailCategorizationService> _logger;

        public EmailCategorizationService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<EmailCategorizationService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Categorizes a single email using the n8n AI webhook
        /// </summary>
        public async Task<bool> CategorizeEmailAsync(Guid emailId)
        {
            var email = await _context.Emails
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == emailId);

            if (email == null)
            {
                _logger.LogWarning("Email {EmailId} not found for categorization", emailId);
                return false;
            }

            // Skip if already categorized
            if (email.CategoryId != null)
            {
                _logger.LogDebug("Email {EmailId} already categorized as {Category}", emailId, email.Category?.Name);
                return true;
            }

            var category = await GetCategoryFromAIAsync(email);
            if (category != null)
            {
                email.CategoryId = category.Id;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Email {EmailId} categorized as {Category}", emailId, category.Name);
                return true;
            }

            _logger.LogWarning("Could not categorize email {EmailId}", emailId);
            return false;
        }

        /// <summary>
        /// Categorizes all uncategorized emails in batches
        /// </summary>
        public async Task<CategorizationResult> CategorizeAllEmailsAsync(
            IProgress<CategorizationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new CategorizationResult();

            // Get webhook configuration
            var settings = await _context.IntegrationSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings == null || !settings.IsWebhookEnabled || string.IsNullOrWhiteSpace(settings.N8nWebhookUrl))
            {
                _logger.LogWarning("Categorization failed: webhook not configured");
                result.ErrorMessage = "n8n webhook is not configured";
                return result;
            }

            // Get all uncategorized emails
            var uncategorizedEmails = await _context.Emails
                .Where(e => e.CategoryId == null)
                .OrderByDescending(e => e.Date)
                .ToListAsync(cancellationToken);

            result.TotalEmails = uncategorizedEmails.Count;

            if (result.TotalEmails == 0)
            {
                _logger.LogInformation("No uncategorized emails found");
                return result;
            }

            _logger.LogInformation("Starting categorization of {Count} emails", result.TotalEmails);

            const int batchSize = 10; // Process in smaller batches to avoid overwhelming the webhook
            var batches = (int)Math.Ceiling(uncategorizedEmails.Count / (double)batchSize);

            for (int batchIndex = 0; batchIndex < batches; batchIndex++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Categorization cancelled after {Count} emails", result.CategorizedCount);
                    break;
                }

                var batch = uncategorizedEmails
                    .Skip(batchIndex * batchSize)
                    .Take(batchSize)
                    .ToList();

                foreach (var email in batch)
                {
                    try
                    {
                        var category = await GetCategoryFromAIAsync(email);
                        if (category != null)
                        {
                            email.CategoryId = category.Id;
                            result.CategorizedCount++;
                        }
                        else
                        {
                            result.FailedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error categorizing email {EmailId}", email.Id);
                        result.FailedCount++;
                    }

                    // Report progress
                    var processed = result.CategorizedCount + result.FailedCount;
                    var progressPercentage = (int)((processed / (double)result.TotalEmails) * 100);
                    progress?.Report(new CategorizationProgress
                    {
                        TotalEmails = result.TotalEmails,
                        ProcessedEmails = processed,
                        CategorizedCount = result.CategorizedCount,
                        FailedCount = result.FailedCount,
                        ProgressPercentage = progressPercentage
                    });

                    // Small delay to avoid overwhelming the webhook
                    await Task.Delay(100, cancellationToken);
                }

                // Save batch changes
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Processed batch {Batch}/{TotalBatches}", batchIndex + 1, batches);
            }

            _logger.LogInformation(
                "Categorization complete: {Categorized} categorized, {Failed} failed out of {Total}",
                result.CategorizedCount, result.FailedCount, result.TotalEmails);

            return result;
        }

        /// <summary>
        /// Calls the n8n AI webhook to determine the category for an email
        /// </summary>
        public async Task<Category?> GetCategoryFromAIAsync(Email email)
        {
            try
            {
                // Get webhook configuration
                var settings = await _context.IntegrationSettings.FirstOrDefaultAsync();
                if (settings == null || !settings.IsWebhookEnabled || string.IsNullOrWhiteSpace(settings.N8nWebhookUrl))
                {
                    _logger.LogWarning("Webhook not configured, using default category");
                    return await GetDefaultCategoryAsync();
                }

                // Get all available categories
                var categories = await _context.Categories
                    .Select(c => c.Name)
                    .ToListAsync();

                // Prepare email data for AI
                var emailData = new
                {
                    subject = email.Subject ?? "",
                    from = email.From ?? "",
                    to = email.To ?? "",
                    textBody = email.TextBody ?? "",
                    htmlBody = email.HtmlBody ?? "",
                    date = email.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                    availableCategories = categories // Send categories to n8n
                };

                // Call n8n webhook
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var response = await httpClient.PostAsJsonAsync(settings.N8nWebhookUrl, emailData);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Webhook call failed with status {Status}", response.StatusCode);
                    return await GetDefaultCategoryAsync();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<N8nCategorizationResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (aiResponse == null || string.IsNullOrWhiteSpace(aiResponse.CategoryName))
                {
                    _logger.LogWarning("Invalid AI response format");
                    return await GetDefaultCategoryAsync();
                }

                // Find matching category (case-insensitive)
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => EF.Functions.ILike(c.Name, aiResponse.CategoryName));

                if (category == null)
                {
                    _logger.LogWarning("Category '{CategoryName}' not found in database, using default", aiResponse.CategoryName);
                    return await GetDefaultCategoryAsync();
                }

                _logger.LogDebug("AI categorized email as '{Category}' with {Confidence}% confidence",
                    category.Name, aiResponse.Confidence);

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI categorization webhook");
                return await GetDefaultCategoryAsync();
            }
        }

        /// <summary>
        /// Gets the default category (typically "Uncategorized")
        /// </summary>
        private async Task<Category?> GetDefaultCategoryAsync()
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.IsDefault);
        }

        /// <summary>
        /// Strips HTML tags from text (basic implementation)
        /// </summary>
        private string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Simple regex-based HTML stripping (for more robust parsing, use HtmlAgilityPack)
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
    }

    /// <summary>
    /// Expected response format from n8n webhook
    /// </summary>
    public class N8nCategorizationResponse
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Confidence { get; set; } = 0;
        public string Reasoning { get; set; } = string.Empty;
    }

    /// <summary>
    /// Progress information for bulk categorization
    /// </summary>
    public class CategorizationProgress
    {
        public int TotalEmails { get; set; }
        public int ProcessedEmails { get; set; }
        public int CategorizedCount { get; set; }
        public int FailedCount { get; set; }
        public int ProgressPercentage { get; set; }
    }

    /// <summary>
    /// Result of bulk categorization operation
    /// </summary>
    public class CategorizationResult
    {
        public int TotalEmails { get; set; }
        public int CategorizedCount { get; set; }
        public int FailedCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsSuccess => CategorizedCount > 0 && string.IsNullOrEmpty(ErrorMessage);
    }
}
