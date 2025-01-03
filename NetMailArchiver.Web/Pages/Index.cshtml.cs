using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.Controllers;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public IndexModel(ApplicationDbContext context,
            IToastNotification toastNotification)
        {
            _context = context;
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

        public IActionResult OnPost(string Id)
        {
            var cImapInformation = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(Id)));
            var cImapController = new ImapController(cImapInformation, _context);
            cImapController.ConnectAndAuthenticate();
            var archivedTask = cImapController.ArchiveAllMails();
            if (archivedTask.IsCompletedSuccessfully) _toastNotification.AddSuccessToastMessage("Archived successfully!");
            else _toastNotification.AddErrorToastMessage($"Archived failed: {archivedTask.Exception}");

            return RedirectToPage();
        }
    }
}