using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.ImapConfigs
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public EditModel(ApplicationDbContext context,
            IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }

        [BindProperty]
        public ImapInformation cImapInformation { get; set; }

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Id))
            {
                _toastNotification.AddErrorToastMessage("Id is null");
                return RedirectToPage("/ImapConfigs/Index");
            }

            cImapInformation = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(Id)));

            if (cImapInformation == null)
            {
                _toastNotification.AddErrorToastMessage("Object is null");
                return RedirectToPage("/ImapConfigs/Index");
            }

            return Page();
        }

        public IActionResult OnPost(ImapInformation cImapInformation)
        {
            ModelState.Remove("cImapInformation.Id");
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = _context.ImapInformations.SingleOrDefault(x => x.Id == cImapInformation.Id);

            if (existing == null)
            {
                _toastNotification.AddErrorToastMessage("Mail Account not found.");
                return RedirectToPage("/ImapConfigs/Index");
            }

            // Update properties
            existing.Host = cImapInformation.Host;
            existing.Port = cImapInformation.Port;
            existing.Username = cImapInformation.Username;
            existing.Password = cImapInformation.Password;
            existing.UseSsl = cImapInformation.UseSsl;
            existing.AutoArchive = cImapInformation.AutoArchive;
            existing.ArchiveInterval = cImapInformation.ArchiveInterval;

            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Successfully edited Mail Account.");
            return RedirectToPage("/ImapConfigs/Index");
        }
    }
}
