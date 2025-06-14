using Microsoft.Extensions.Hosting;

namespace NetMailArchiver.Services
{
    public class QuartzStartupService : IHostedService
    {
        private readonly QuartzJobSchedulerService _jobScheduler;

        public QuartzStartupService(QuartzJobSchedulerService jobScheduler)
        {
            _jobScheduler = jobScheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _jobScheduler.InitializeAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _jobScheduler.ShutdownAsync(cancellationToken);
        }
    }

}
