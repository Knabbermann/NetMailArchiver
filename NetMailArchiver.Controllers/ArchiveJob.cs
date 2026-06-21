using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetMailArchiver.DataAccess;
using Quartz;

namespace NetMailArchiver.Services
{
    [DisallowConcurrentExecution]
    public class ArchiveJob : IJob
    {
        private readonly ArchiveLockService _archiveLockService;
        private readonly ILogger<ArchiveJob> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IArchiveProgressService _progressService;
        private readonly IEmailCategorizationService _categorizationService;

        public ArchiveJob(
            ArchiveLockService archiveLockService,
            ILogger<ArchiveJob> logger,
            ApplicationDbContext context,
            IArchiveProgressService progressService,
            IEmailCategorizationService categorizationService)
        {
            _archiveLockService = archiveLockService;
            _logger = logger;
            _context = context;
            _progressService = progressService;
            _categorizationService = categorizationService;
        }

        public async Task Execute(IJobExecutionContext context1)
        {
            var imapIdStr = context1.JobDetail.JobDataMap.GetString("Id");
            if (!Guid.TryParse(imapIdStr, out var imapId))
            {
                _logger.LogError("Ungültige IMAP-ID.");
                return;
            }

            var imapInfo = await _context.ImapInformations.SingleOrDefaultAsync(x => x.Id == imapId);
            if (imapInfo == null)
            {
                _logger.LogError("IMAP Information mit ID {ImapId} nicht gefunden.", imapId);
                return;
            }

            var imapIdString = imapId.ToString();
            _progressService.SetJobRunning(imapIdString, true);
            _progressService.SetProgress(imapIdString, 0);

            _logger.LogInformation("Starte Archivierung für IMAP-ID {ImapId}", imapId);

            using var imapController = new ImapService(_archiveLockService, imapInfo, _context);

            try
            {
                imapController.ConnectAndAuthenticate();

                var progress = new Progress<int>(percent =>
                {
                    _progressService.SetProgress(imapIdString, percent);
                });

                await imapController.ArchiveNewMails(progress, cancellationToken: CancellationToken.None);

                // Categorize newly archived emails
                await CategorizeNewEmailsAsync(imapId);

                _progressService.SetProgress(imapIdString, 100);
                _logger.LogInformation("Archivierung abgeschlossen.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Archivierung");
                _progressService.SetProgress(imapIdString, -1);
            }
            finally
            {
                // Force garbage collection after job completion
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Remove progress after a short delay to allow frontend to see completion
                await Task.Delay(5000);
                _progressService.RemoveProgress(imapIdString);
            }
        }

        private async Task CategorizeNewEmailsAsync(Guid imapId)
        {
            try
            {
                // Check if webhook is enabled
                var settings = await _context.IntegrationSettings.FirstOrDefaultAsync();
                if (settings == null || !settings.IsWebhookEnabled || string.IsNullOrWhiteSpace(settings.N8nWebhookUrl))
                {
                    _logger.LogInformation("Auto-categorization skipped: webhook not configured");
                    return;
                }

                // Get uncategorized emails from this IMAP account
                var uncategorizedEmails = await _context.Emails
                    .Where(e => e.ImapInformationId == imapId && e.CategoryId == null)
                    .OrderByDescending(e => e.Date)
                    .Take(50) // Limit to most recent 50 emails per archive run
                    .ToListAsync();

                if (!uncategorizedEmails.Any())
                {
                    _logger.LogInformation("No uncategorized emails to process");
                    return;
                }

                _logger.LogInformation("Categorizing {Count} newly archived emails", uncategorizedEmails.Count);

                foreach (var email in uncategorizedEmails)
                {
                    try
                    {
                        await _categorizationService.CategorizeEmailAsync(email.Id);
                        // Small delay to avoid overwhelming the webhook
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to categorize email {EmailId}", email.Id);
                    }
                }

                _logger.LogInformation("Email categorization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email categorization");
            }
        }
    }
}
