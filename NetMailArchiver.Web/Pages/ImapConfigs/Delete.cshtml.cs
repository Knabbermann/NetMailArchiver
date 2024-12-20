using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.ImapConfigs
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public DeleteModel(ApplicationDbContext context,
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

        public IActionResult OnPost()
        {
            var imapInformationToDelete = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(Id)));

            if (imapInformationToDelete == null)
            {
                _toastNotification.AddErrorToastMessage("Object not found");
                return RedirectToPage("/ImapConfigs/Index");
            }

            _context.ImapInformations.Remove(imapInformationToDelete);
            _context.SaveChanges();

            _toastNotification.AddSuccessToastMessage("Successfully removed Mail Account.");
            return RedirectToPage("/ImapConfigs/Index");
        }
    }
}
