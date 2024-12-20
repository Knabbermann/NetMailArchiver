using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.Controllers;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.ImapConfigs
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
        }

        public IActionResult OnPost(string Id)
        {
            var cImapInformation = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(Id)));
            var cImapController = new ImapController(cImapInformation);
            cImapController.ConnectAndAuthenticate();
            var isConnectedAndAuthenticated = cImapController.IsConnectedAndAuthenticated();
            if (isConnectedAndAuthenticated) _toastNotification.AddSuccessToastMessage("Mail Account connection succesfull!");
            else _toastNotification.AddErrorToastMessage("Mail Account connection failed!");
            
            return RedirectToPage();
        }
    }
}
