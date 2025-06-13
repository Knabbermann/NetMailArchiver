using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetMailArchiver.DataAccess;
using Quartz;

namespace NetMailArchiver.Services
{
    public class QuartzStartupService(
        IServiceProvider serviceProvider,
        ISchedulerFactory schedulerFactory,
        ILogger<QuartzStartupService> logger)
        : IHostedService
    {
        private IScheduler? _scheduler;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var imapInformations = await context.ImapInformations.ToListAsync(cancellationToken);

            foreach (var imap in imapInformations.Where(i => i.AutoArchive))
            {
                var job = JobBuilder.Create<ArchiveJob>()
                    .WithIdentity($"ArchiveJob_{imap.Id}", "ArchiveGroup")
                    .UsingJobData("Id", imap.Id.ToString())
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"ArchiveTrigger_{imap.Id}", "ArchiveGroup")
                    .StartNow()
                    .WithCronSchedule(imap.ArchiveInterval)
                    .Build();

                await _scheduler.ScheduleJob(job, trigger, cancellationToken);
                logger.LogInformation($"Scheduled ArchiveJob for IMAP-ID {imap.Id} with interval {imap.ArchiveInterval}.");
            }

            await _scheduler.Start(cancellationToken);
            logger.LogInformation("Quartz scheduler started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null)
                await _scheduler.Shutdown(cancellationToken);
        }
    }
}
