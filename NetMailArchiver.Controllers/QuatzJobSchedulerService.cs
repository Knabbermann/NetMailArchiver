using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetMailArchiver.DataAccess;
using Quartz;

namespace NetMailArchiver.Services
{
    public class QuartzJobSchedulerService(
        IServiceProvider serviceProvider,
        ISchedulerFactory schedulerFactory,
        ILogger<QuartzJobSchedulerService> logger)
    {
        private IScheduler? _scheduler;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _scheduler ??= await schedulerFactory.GetScheduler(cancellationToken);
            if (!_scheduler.InStandbyMode)
                await _scheduler.Clear(cancellationToken);

            await ScheduleAllJobsAsync(cancellationToken);
            await _scheduler.Start(cancellationToken);
        }

        public async Task ScheduleAllJobsAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var imapInformations = await context.ImapInformations.ToListAsync(cancellationToken);

            foreach (var imap in imapInformations.Where(i => i.AutoArchive))
            {
                var jobKey = new JobKey($"ArchiveJob_{imap.Id}", "ArchiveGroup");
                var triggerKey = new TriggerKey($"ArchiveTrigger_{imap.Id}", "ArchiveGroup");

                var job = JobBuilder.Create<ArchiveJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("Id", imap.Id.ToString())
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .StartNow()
                    .WithCronSchedule(imap.ArchiveInterval)
                    .Build();

                await _scheduler.ScheduleJob(job, trigger, cancellationToken);
                logger.LogInformation($"Scheduled job for IMAP-ID {imap.Id} with interval {imap.ArchiveInterval}.");
            }
        }

        public async Task ReloadScheduleAsync(CancellationToken cancellationToken)
        {
            if (_scheduler == null)
            {
                logger.LogWarning("Scheduler is not initialized.");
                return;
            }

            logger.LogInformation("Reloading all scheduled jobs...");
            await _scheduler.Clear(cancellationToken);
            await ScheduleAllJobsAsync(cancellationToken);
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null)
                await _scheduler.Shutdown(cancellationToken);
        }
    }

}
