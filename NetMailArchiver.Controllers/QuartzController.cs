using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using Quartz;
using Quartz.Impl;

namespace NetMailArchiver.Controllers
{
    public class QuartzJobController
    {
        private IScheduler _scheduler;
        private IJobDetail _jobDetail;
        private readonly ApplicationDbContext _context;

        public QuartzJobController(ApplicationDbContext context)
        {
            // Scheduler initialisieren
            var schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler().Result;
            _context = context;

            // Jobs für jede IMAP-Information starten, bei denen AutoArchivierung aktiv ist
            var imapInformations = _context.ImapInformations.ToList();
            foreach (var imapInformation in imapInformations)
            {
                if (imapInformation.AutoArchive)
                {
                    StartJobAsync(imapInformation);
                }
            }
        }

        // Methode zum Starten eines Jobs basierend auf den ImapInformationen
        public async Task StartJobAsync(ImapInformation imapInformation)
        {
            // Erstellen des Jobdetails für jeden Job
            _jobDetail = JobBuilder.Create<ArchiveJob>()
                                   .WithIdentity($"ArchiveJob_{imapInformation.Id}", "ArchiveGroup")
                                   .UsingJobData("Id", imapInformation.Id)
                                   .Build();

            // Erstellen des Triggers mit einem dynamischen Intervall aus der Datenbank
            var trigger = TriggerBuilder.Create()
                                        .WithIdentity($"ArchiveTrigger_{imapInformation.Id}", "ArchiveGroup")
                                        .StartNow()
                                        .WithSimpleSchedule(x => x
                                            .WithInterval(TimeSpan.FromMinutes(imapInformation.ArchiveInterval))  // Archivierungsintervall
                                            .RepeatForever())
                                        .Build();

            // Job und Trigger dem Scheduler hinzufügen
            await _scheduler.ScheduleJob(_jobDetail, trigger);
            await _scheduler.Start();
        }

        // Methode zum Ändern des Intervalls eines bestehenden Jobs
        public async Task ChangeJobIntervalAsync(int jobId, TimeSpan newInterval)
        {
            var jobKey = new JobKey($"ArchiveJob_{jobId}", "ArchiveGroup");
            var triggerKey = new TriggerKey($"ArchiveTrigger_{jobId}", "ArchiveGroup");

            if (await _scheduler.CheckExists(jobKey))
            {
                // Trigger mit neuem Intervall erstellen
                var newTrigger = TriggerBuilder.Create()
                                               .WithIdentity(triggerKey)
                                               .StartNow()
                                               .WithSimpleSchedule(x => x
                                                   .WithInterval(newInterval)
                                                   .RepeatForever())
                                               .Build();

                // Trigger des Jobs aktualisieren
                await _scheduler.RescheduleJob(triggerKey, newTrigger);
            }
        }

        // Methode zum Pausieren des Jobs
        public async Task PauseJobAsync(int jobId)
        {
            var jobKey = new JobKey($"ArchiveJob_{jobId}", "ArchiveGroup");

            if (await _scheduler.CheckExists(jobKey))
            {
                await _scheduler.PauseJob(jobKey);
            }
        }

        // Methode zum Fortsetzen des Jobs
        public async Task ResumeJobAsync(int jobId)
        {
            var jobKey = new JobKey($"ArchiveJob_{jobId}", "ArchiveGroup");

            if (await _scheduler.CheckExists(jobKey))
            {
                await _scheduler.ResumeJob(jobKey);
            }
        }
    }

    // Beispielhafte Job-Klasse für die Archivierung (wird bei der Job-Ausführung aufgerufen)
    public class ArchiveJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var imapId = context.JobDetail.JobDataMap.GetString("Id");

            return Task.CompletedTask;
        }
    }
}
