using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.Services;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages
{
    public class IndexModel(ArchiveLockService archiveLockService,
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        IToastNotification toastNotification,
        IArchiveProgressService progressService)
        : PageModel
    {
        public IEnumerable<ImapInformation> ImapInformations { get; set; }

        public void OnGet()
        {
            ImapInformations = context.ImapInformations.ToList();
            foreach (var imapInformation in ImapInformations)
            {
                imapInformation.EmailCount = context.Emails.Count(x => x.ImapInformationId.Equals(imapInformation.Id));
                imapInformation.AttachmentCount = context.Attachments.Count(x => x.Email.ImapInformationId.Equals(imapInformation.Id));
            }
        }

        public IActionResult OnGetArchiveNewMails(string id)
        {
            var cImapInformation = context.ImapInformations.Single(x => x.Id.Equals(new Guid(id)));

            progressService.SetProgress(id, 0);
            progressService.SetJobRunning(id, true);
            toastNotification.AddInfoToastMessage("Started archiving new mails.");

            Task.Run(async () =>
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cImapControllerInTask = new ImapService(archiveLockService, cImapInformation, scopedContext);

                    try
                    {
                        cImapControllerInTask.ConnectAndAuthenticate();
                        await cImapControllerInTask.ArchiveNewMails(new Progress<int>(progress =>
                        {
                            progressService.SetProgress(id, progress);
                        }), CancellationToken.None);

                        progressService.SetProgress(id, 100);
                    }
                    catch (Exception ex)
                    {
                        toastNotification.AddErrorToastMessage(ex.Message);
                        progressService.SetProgress(id, -1);
                    }
                    finally
                    {
                        // Remove progress after a short delay to allow frontend to see completion
                        await Task.Delay(5000);
                        progressService.RemoveProgress(id);
                    }
                    toastNotification.AddSuccessToastMessage("Finished archiving new mails.");
                }
            });

            return new JsonResult(new { status = "started" });
        }

        public IActionResult OnGetArchiveAllMails(string id)
        {
            var cImapInformation = context.ImapInformations.Single(x => x.Id.Equals(new Guid(id)));

            progressService.SetProgress(id, 0);
            progressService.SetJobRunning(id, true);
            toastNotification.AddInfoToastMessage("Started archiving all mails.");

            Task.Run(async () =>
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cImapControllerInTask = new ImapService(archiveLockService, cImapInformation, scopedContext);

                    try
                    {
                        cImapControllerInTask.ConnectAndAuthenticate();
                        await cImapControllerInTask.ArchiveAllMails(new Progress<int>(progress =>
                        {
                            progressService.SetProgress(id, progress);
                        }), CancellationToken.None);

                        progressService.SetProgress(id, 100);
                    }
                    catch (Exception ex)
                    {
                        toastNotification.AddErrorToastMessage(ex.Message);
                        progressService.SetProgress(id, -1); 
                    }
                    finally
                    {
                        // Remove progress after a short delay to allow frontend to see completion
                        await Task.Delay(5000);
                        progressService.RemoveProgress(id);
                    }
                    toastNotification.AddInfoToastMessage("Finished archiving all mails.");
                }
            });

            return new JsonResult(new { status = "started" });
        }

        public IActionResult OnGetArchiveProgress(string id)
        {
            var progress = progressService.GetProgress(id);
            var isRunning = progressService.IsJobRunning(id);
            return new JsonResult(new { progress = progress, isRunning = isRunning });
        }

        public IActionResult OnGetActiveJobs()
        {
            var activeJobs = progressService.GetActiveJobs();
            return new JsonResult(activeJobs);
        }
    }
}
