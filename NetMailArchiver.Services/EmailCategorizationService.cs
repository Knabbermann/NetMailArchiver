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

        public async Task<bool> CategorizeEmailAsync(Guid emailId)
        {
            try
            {
                var email = await _context.Emails
                    .Include(e => e.Category)
                    .FirstOrDefaultAsync(e => e.Id == emailId);

                if (email == null)
                {
                    _logger.LogWarning("Email with ID {EmailId} not found", emailId);
                    return false;
                }

                // Skip if already categorized (unless it's the default category)
                if (email.CategoryId.HasValue && email.Category?.IsDefault == false)
                {
                    _logger.LogInformation("Email {EmailId} already categorized as {CategoryName}", emailId, email.Category.Name);
                    return true;
                }

                var category = await GetCategoryFromAIAsync(email);

                if (category != null)
                {
                    email.CategoryId = category.Id;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Email {EmailId} categorized as {CategoryName}", emailId, category.Name);
                    return true;
                }

                // Fallback to default category
                var defaultCategory = await _context.Categories.FirstOrDefaultAsync(c => c.IsDefault);
                if (defaultCategory != null)
                {
                    email.CategoryId = defaultCategory.Id;
                    await _context.SaveChangesAsync();
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error categorizing email {EmailId}", emailId);
                return false;
            }
        }

        public async Task<CategorizationResult> CategorizeAllEmailsAsync(
            IProgress<CategorizationProgress>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            var result = new CategorizationResult();

            try
            {
                // Get all uncategorized emails or emails with default category
                var defaultCategory = await _context.Categories.FirstOrDefaultAsync(c => c.IsDefault, cancellationToken);

                var emailsToProcess = await _context.Emails
                    .Where(e => e.CategoryId == null || e.CategoryId == defaultCategory!.Id)
                    .OrderByDescending(e => e.Date)
                    .ToListAsync(cancellationToken);

                result.TotalEmails = emailsToProcess.Count;

                _logger.LogInformation("Starting categorization of {Count} emails", result.TotalEmails);

                for (int i = 0; i < emailsToProcess.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.IsCancelled = true;
                        break;
                    }

                    var email = emailsToProcess[i];

                    try
                    {
                        var category = await GetCategoryFromAIAsync(email);

                        if (category != null)
                        {
                            email.CategoryId = category.Id;
                            result.SuccessCount++;
                        }
                        else
                        {
                            // Keep default category
                            result.FailedCount++;
                        }

                        // Report progress every 10 emails or on last email
                        if ((i + 1) % 10 == 0 || i == emailsToProcess.Count - 1)
                        {
                            await _context.SaveChangesAsync(cancellationToken);

                            progress?.Report(new CategorizationProgress
                            {
                                ProcessedCount = i + 1,
                                TotalCount = result.TotalEmails,
                                SuccessCount = result.SuccessCount,
                                FailedCount = result.FailedCount,
                                CurrentEmailSubject = email.Subject ?? "No subject"
                            });
                        }

                        // Small delay to avoid overwhelming the AI service
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error categorizing email {EmailId}", email.Id);
                        result.FailedCount++;
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                result.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Categorization completed: {Success} successful, {Failed} failed out of {Total}", 
                    result.SuccessCount, result.FailedCount, result.TotalEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk categorization");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<Category?> GetCategoryFromAIAsync(Email email)
        {
            try
            {
                var settings = await _context.IntegrationSettings.FirstOrDefaultAsync();

                if (settings == null || string.IsNullOrWhiteSpace(settings.N8nWebhookUrl) || !settings.IsWebhookEnabled)
                {
                    _logger.LogWarning("n8n webhook not configured or disabled");
                    return null;
                }

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var payload = new
                {
                    emailId = email.Id,
                    subject = email.Subject ?? "",
                    from = email.From ?? "",
                    to = email.To ?? "",
                    textBody = email.TextBody ?? "",
                    date = email.Date
                };

                var response = await httpClient.PostAsJsonAsync(settings.N8nWebhookUrl, payload);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var aiResponse = JsonSerializer.Deserialize<N8nCategorizationResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.CategoryName))
                    {
                        // Try to find category by name (case-insensitive)
                        var category = await _context.Categories
                            .FirstOrDefaultAsync(c => c.Name.ToLower() == aiResponse.CategoryName.ToLower());

                        if (category != null)
                        {
                            return category;
                        }

                        _logger.LogWarning("AI suggested category '{CategoryName}' not found in database", aiResponse.CategoryName);
                    }
                }
                else
                {
                    _logger.LogWarning("n8n webhook returned status {StatusCode}", response.StatusCode);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("n8n webhook request timed out for email {EmailId}", email.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling n8n webhook for email {EmailId}", email.Id);
            }

            return null;
        }
    }

    // Response model from n8n
    public class N8nCategorizationResponse
    {
        public string? CategoryName { get; set; }
        public float? Confidence { get; set; }
        public string? Reasoning { get; set; }
    }

    // Progress reporting
    public class CategorizationProgress
    {
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string CurrentEmailSubject { get; set; } = string.Empty;
        public int PercentComplete => TotalCount > 0 ? (ProcessedCount * 100 / TotalCount) : 0;
    }

    // Result model
    public class CategorizationResult
    {
        public int TotalEmails { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
