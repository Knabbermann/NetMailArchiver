using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetMailArchiver.DataAccess;
using Quartz;

namespace NetMailArchiver.Services
{
    [DisallowConcurrentExecution]
    public class ArchiveJob(ArchiveLockService archiveLockService, ILogger<ArchiveJob> logger, ApplicationDbContext context) : IJob
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

            logger.LogInformation($"Starte Archivierung für IMAP-ID {imapId}");
            try
            {
                var imapController = new ImapService(archiveLockService, imapInfo, context);
                imapController.ConnectAndAuthenticate();
                await imapController.ArchiveNewMails(null,cancellationToken: CancellationToken.None);
                logger.LogInformation("Archivierung abgeschlossen.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler bei der Archivierung");
            }
        }
    }

}
