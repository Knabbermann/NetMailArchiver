using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.Services;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.Integrations
{
    public class IndexModel(
        ArchiveLockService archiveLockService,
        ApplicationDbContext context,
        IToastNotification toastNotification,
        IEmailCategorizationService categorizationService)
        : PageModel
    {
        public IEnumerable<ImapInformation> ImapInformations { get; set; } = [];

        [BindProperty]
        public IntegrationSettings IntegrationSettings { get; set; } = new();

        public int TotalCategories { get; set; }
        public int CategorizedEmailsCount { get; set; }
        public int UncategorizedEmailsCount { get; set; }

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
            }
            catch (Exception)
            {
                // Ensure defaults are set even if queries fail
                TotalCategories = 0;
                CategorizedEmailsCount = 0;
                UncategorizedEmailsCount = 0;
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

            toastNotification.AddInfoToastMessage("Email categorization started in background. This may take a while...");

            // Start categorization in background (fire and forget)
            _ = Task.Run(async () =>
            {
                await categorizationService.CategorizeAllEmailsAsync();
            });

            return RedirectToPage();
        }
    }
}
