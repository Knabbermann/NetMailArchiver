using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.Services;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NetMailArchiver.Web.Services;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.Integrations
{
    public class IndexModel(
        ArchiveLockService archiveLockService,
        ApplicationDbContext context,
        IToastNotification toastNotification,
        IEmailCategorizationService categorizationService,
        IOperationCancellationService cancellationService,
        ICategorizationProgressService progressService)
        : PageModel
    {
        private const string BulkCategorizationOperationId = "bulk-categorization";

        public IEnumerable<ImapInformation> ImapInformations { get; set; } = [];

        [BindProperty]
        public IntegrationSettings IntegrationSettings { get; set; } = new();

        public int TotalCategories { get; set; }
        public int CategorizedEmailsCount { get; set; }
        public int UncategorizedEmailsCount { get; set; }
        public bool IsBulkCategorizationRunning { get; set; }
        public CategorizationProgress? CurrentProgress { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Load Mail Accounts
                ImapInformations = await context.ImapInformations.ToListAsync();

                // Calculate EmailCount and AttachmentCount for each IMAP configuration
                foreach (var imapInformation in ImapInformations)
                {
                    imapInformation.EmailCount = await context.Emails.CountAsync(x => x.ImapInformationId.Equals(imapInformation.Id));
                    imapInformation.AttachmentCount = await context.Attachments.CountAsync(x => x.Email.ImapInformationId.Equals(imapInformation.Id));
                }

                // Load Integration Settings - ALWAYS ensure it's not null
                var settings = await context.IntegrationSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    IntegrationSettings = new IntegrationSettings
                    {
                        IsWebhookEnabled = false,
                        N8nWebhookUrl = string.Empty,
                        Description = string.Empty
                    };
                }
                else
                {
                    IntegrationSettings = settings;
                }

                // Load categorization statistics
                TotalCategories = await context.Categories.CountAsync();
                CategorizedEmailsCount = await context.Emails.CountAsync(e => e.CategoryId != null);
                UncategorizedEmailsCount = await context.Emails.CountAsync(e => e.CategoryId == null);
                IsBulkCategorizationRunning = cancellationService.IsOperationRunning(BulkCategorizationOperationId);
                CurrentProgress = progressService.GetProgress(BulkCategorizationOperationId);
            }
            catch (Exception)
            {
                // Ensure defaults are set even if queries fail
                TotalCategories = 0;
                CategorizedEmailsCount = 0;
                UncategorizedEmailsCount = 0;
                IsBulkCategorizationRunning = false;
                CurrentProgress = null;
                ImapInformations = [];
                IntegrationSettings = new IntegrationSettings
                {
                    IsWebhookEnabled = false,
                    N8nWebhookUrl = string.Empty,
                    Description = string.Empty
                };
            }
        }

        public async Task<IActionResult> OnPostTestConnectionAsync(string id)
        {
            var cImapInformation = await context.ImapInformations.SingleAsync(x => x.Id.Equals(new Guid(id)));
            var cImapService = new ImapService(archiveLockService, cImapInformation);
            cImapService.ConnectAndAuthenticate();
            var isConnectedAndAuthenticated = cImapService.IsConnectedAndAuthenticated();

            if (isConnectedAndAuthenticated) 
                toastNotification.AddSuccessToastMessage("Mail Account connection successful!");
            else 
                toastNotification.AddErrorToastMessage("Mail Account connection failed!");

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveWebhookAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var existingSettings = await context.IntegrationSettings.FirstOrDefaultAsync();

            if (existingSettings == null)
            {
                IntegrationSettings.UpdatedAt = DateTime.UtcNow;
                context.IntegrationSettings.Add(IntegrationSettings);
                toastNotification.AddSuccessToastMessage("n8n Webhook configuration saved!");
            }
            else
            {
                existingSettings.N8nWebhookUrl = IntegrationSettings.N8nWebhookUrl;
                existingSettings.IsWebhookEnabled = IntegrationSettings.IsWebhookEnabled;
                existingSettings.Description = IntegrationSettings.Description;
                existingSettings.UpdatedAt = DateTime.UtcNow;
                context.IntegrationSettings.Update(existingSettings);
                toastNotification.AddSuccessToastMessage("n8n Webhook configuration updated!");
            }

            await context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTestWebhookAsync()
        {
            var settings = await context.IntegrationSettings.FirstOrDefaultAsync();

            if (settings == null || string.IsNullOrWhiteSpace(settings.N8nWebhookUrl))
            {
                toastNotification.AddWarningToastMessage("Please configure the webhook URL first!");
                return RedirectToPage();
            }

            try
            {
                using var httpClient = new HttpClient();
                var testPayload = new
                {
                    test = true,
                    message = "Test from NetMailArchiver",
                    timestamp = DateTime.UtcNow
                };

                var response = await httpClient.PostAsJsonAsync(settings.N8nWebhookUrl, testPayload);

                if (response.IsSuccessStatusCode)
                {
                    toastNotification.AddSuccessToastMessage($"Webhook test successful! Status: {response.StatusCode}");
                }
                else
                {
                    toastNotification.AddErrorToastMessage($"Webhook test failed! Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                toastNotification.AddErrorToastMessage($"Webhook test error: {ex.Message}");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostStartCategorizationAsync()
        {
            var settings = await context.IntegrationSettings.FirstOrDefaultAsync();

            if (settings == null || string.IsNullOrWhiteSpace(settings.N8nWebhookUrl) || !settings.IsWebhookEnabled)
            {
                toastNotification.AddWarningToastMessage("Please configure and enable the n8n webhook first!");
                return RedirectToPage();
            }

            if (cancellationService.IsOperationRunning(BulkCategorizationOperationId))
            {
                toastNotification.AddWarningToastMessage("Bulk categorization is already running!");
                return RedirectToPage();
            }

            toastNotification.AddInfoToastMessage("Email categorization started in background. This may take a while...");

            // Start categorization in background with cancellation support
            _ = Task.Run(async () =>
            {
                var cancellationToken = cancellationService.GetOrCreateToken(BulkCategorizationOperationId);
                try
                {
                    var progress = new Progress<CategorizationProgress>(p =>
                    {
                        progressService.UpdateProgress(BulkCategorizationOperationId, p);
                    });
                    await categorizationService.CategorizeAllEmailsAsync(progress, cancellationToken);
                }
                finally
                {
                    cancellationService.CompleteOperation(BulkCategorizationOperationId);
                    progressService.ClearProgress(BulkCategorizationOperationId);
                }
            });

            return RedirectToPage();
        }

        public IActionResult OnPostCancelCategorizationAsync()
        {
            if (!cancellationService.IsOperationRunning(BulkCategorizationOperationId))
            {
                toastNotification.AddWarningToastMessage("No bulk categorization is currently running!");
                return RedirectToPage();
            }

            cancellationService.CancelOperation(BulkCategorizationOperationId);
            toastNotification.AddSuccessToastMessage("Bulk categorization cancelled successfully!");

            return RedirectToPage();
        }

        public IActionResult OnGetCategorizationStatus()
        {
            var isRunning = cancellationService.IsOperationRunning(BulkCategorizationOperationId);
            var progress = progressService.GetProgress(BulkCategorizationOperationId);

            return new JsonResult(new 
            { 
                isRunning,
                progress = progress != null ? new
                {
                    totalEmails = progress.TotalEmails,
                    processedEmails = progress.ProcessedEmails,
                    categorizedCount = progress.CategorizedCount,
                    failedCount = progress.FailedCount,
                    progressPercentage = progress.ProgressPercentage
                } : null
            });
        }
    }
}
