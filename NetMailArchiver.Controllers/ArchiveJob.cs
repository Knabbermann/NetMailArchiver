using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetMailArchiver.DataAccess;
using Quartz;

namespace NetMailArchiver.Services
{
    [DisallowConcurrentExecution]
    public class ArchiveJob(ArchiveLockService archiveLockService, ILogger<ArchiveJob> logger, ApplicationDbContext context, IArchiveProgressService progressService) : IJob
    {
        public async Task Execute(IJobExecutionContext context1)
        {
            var imapIdStr = context1.JobDetail.JobDataMap.GetString("Id");
            if (!Guid.TryParse(imapIdStr, out var imapId))
            {
                logger.LogError("Ungültige IMAP-ID.");
                return;
            }

            var imapInfo = await context.ImapInformations.SingleOrDefaultAsync(x => x.Id == imapId);
            if (imapInfo == null)
            {
                logger.LogError($"IMAP Information mit ID {imapId} nicht gefunden.");
                return;
            }

            var imapIdString = imapId.ToString();
            progressService.SetJobRunning(imapIdString, true);
            progressService.SetProgress(imapIdString, 0);

            logger.LogInformation($"Starte Archivierung für IMAP-ID {imapId}");
            try
            {
                var imapController = new ImapService(archiveLockService, imapInfo, context);
                imapController.ConnectAndAuthenticate();
                
                var progress = new Progress<int>(percent =>
                {
                    progressService.SetProgress(imapIdString, percent);
                });
                
                await imapController.ArchiveNewMails(progress, cancellationToken: CancellationToken.None);
                
                progressService.SetProgress(imapIdString, 100);
                logger.LogInformation("Archivierung abgeschlossen.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler bei der Archivierung");
                progressService.SetProgress(imapIdString, -1);
            }
            finally
            {
                // Remove progress after a short delay to allow frontend to see completion
                await Task.Delay(5000);
                progressService.RemoveProgress(imapIdString);
            }
        }
    }
}
