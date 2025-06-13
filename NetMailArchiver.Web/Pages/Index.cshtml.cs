using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.Services;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;
using System.Collections.Concurrent;

namespace NetMailArchiver.Web.Pages
{
    public class IndexModel(ArchiveLockService archiveLockService,
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        IToastNotification toastNotification)
        : PageModel
    {
        private static ConcurrentDictionary<string, int> _progressDictionary = new ConcurrentDictionary<string, int>();

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

            _progressDictionary[id] = 0;
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
                            _progressDictionary[id] = progress;
                        }), CancellationToken.None);

                        _progressDictionary[id] = 100;
                    }
                    catch (Exception ex)
                    {
                        toastNotification.AddErrorToastMessage(ex.Message);
                        _progressDictionary[id] = -1;
                    }
                    toastNotification.AddSuccessToastMessage("Finished archiving new mails.");
                }
            });

            return new JsonResult(new { status = "started" });
        }

        public IActionResult OnGetArchiveAllMails(string id)
        {
            var cImapInformation = context.ImapInformations.Single(x => x.Id.Equals(new Guid(id)));

            _progressDictionary[id] = 0;
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
                            _progressDictionary[id] = progress;
                        }), CancellationToken.None);

                        _progressDictionary[id] = 100;
                    }
                    catch (Exception ex)
                    {
                        toastNotification.AddErrorToastMessage(ex.Message);
                        _progressDictionary[id] = -1; 
                    }
                    toastNotification.AddInfoToastMessage("Finished archiving all mails.");
                }
            });

            return new JsonResult(new { status = "started" });
        }


        public IActionResult OnGetArchiveProgress(string id)
        {
            return _progressDictionary.TryGetValue(id, out var progress) ? new JsonResult(progress) : new JsonResult(0);
        }
    }
}
