using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.Services;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.ImapConfigs
{
    public class IndexModel(ArchiveLockService archiveLockService,
        ApplicationDbContext context,
        IToastNotification toastNotification)
        : PageModel
    {
        public IEnumerable<ImapInformation> ImapInformations { get; set; }

        public void OnGet()
        {
            ImapInformations = context.ImapInformations.ToList();
        }

        public IActionResult OnPost(string id)
        {
            var cImapInformation = context.ImapInformations.Single(x => x.Id.Equals(new Guid(id)));
            var cImapService = new ImapService(archiveLockService, cImapInformation);
            cImapService.ConnectAndAuthenticate();
            var isConnectedAndAuthenticated = cImapService.IsConnectedAndAuthenticated();
            if (isConnectedAndAuthenticated) toastNotification.AddSuccessToastMessage("Mail Account connection successfull!");
            else toastNotification.AddErrorToastMessage("Mail Account connection failed!");

            return RedirectToPage();
        }
    }
}
