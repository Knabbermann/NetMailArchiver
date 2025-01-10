using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.Controllers;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;
using System.Collections.Concurrent;

namespace NetMailArchiver.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly IToastNotification _toastNotification;
        private static ConcurrentDictionary<string, int> ProgressDictionary = new ConcurrentDictionary<string, int>();

        public IndexModel(ApplicationDbContext context,
            IServiceProvider serviceProvider,
            IToastNotification toastNotification)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _toastNotification = toastNotification;
        }

        public IEnumerable<ImapInformation> ImapInformations { get; set; }

        public void OnGet()
        {
            ImapInformations = _context.ImapInformations.ToList();
            foreach (var imapInformation in ImapInformations)
            {
                imapInformation.EmailCount = _context.Emails.Count(x => x.ImapInformationId.Equals(imapInformation.Id));
                imapInformation.AttachmentCount = _context.Attachments.Count(x => x.Email.ImapInformationId.Equals(imapInformation.Id));
            }
        }

        public IActionResult OnGetArchiveNewMails(string id)
        {
            var cImapInformation = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(id)));
            var cImapController = new ImapController(cImapInformation, _context);

            ProgressDictionary[id] = 0;

            Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cImapControllerInTask = new ImapController(cImapInformation, scopedContext);

                    try
                    {
                        cImapControllerInTask.ConnectAndAuthenticate();
                        await cImapControllerInTask.ArchiveNewMails(new Progress<int>(progress =>
                        {
                            ProgressDictionary[id] = progress;
                        }), CancellationToken.None);

                        ProgressDictionary[id] = 100;
                    }
                    catch (Exception ex)
                    {
                        _toastNotification.AddErrorToastMessage(ex.Message);
                        ProgressDictionary[id] = -1; // Optional: Fehlerstatus
                    }
                }
            });

            return new JsonResult(new { status = "started" });
        }

        public IActionResult OnGetArchiveAllMails(string id)
        {
            var cImapInformation = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(id)));
            var cImapController = new ImapController(cImapInformation, _context);

            ProgressDictionary[id] = 0;

            Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cImapControllerInTask = new ImapController(cImapInformation, scopedContext);

                    try
                    {
                        cImapControllerInTask.ConnectAndAuthenticate();
                        await cImapControllerInTask.ArchiveAllMails(new Progress<int>(progress =>
                        {
                            ProgressDictionary[id] = progress;
                        }), CancellationToken.None);

                        ProgressDictionary[id] = 100;
                    }
                    catch (Exception ex)
                    {
                        _toastNotification.AddErrorToastMessage(ex.Message);
                        ProgressDictionary[id] = -1; // Optional: Fehlerstatus
                    }
                }
            });

            return new JsonResult(new { status = "started" });
        }


        public IActionResult OnGetArchiveProgress(string id)
        {
            if (ProgressDictionary.TryGetValue(id, out var progress))
            {
                return new JsonResult(progress);
            }
            return new JsonResult(0);
        }
    }
}
