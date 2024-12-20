using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
            if (ModelState.IsValid)
            {
                var oldImapInformation = _context.ImapInformations.Single(x => x.Id.Equals(new Guid(Id)));
                _context.ImapInformations.Remove(oldImapInformation);
                _context.SaveChanges();
                _context.ImapInformations.Add(cImapInformation);
                _context.SaveChanges();
                _toastNotification.AddSuccessToastMessage("Successfully edited Mail Account.");
                return RedirectToPage("/ImapConfigs/Index");
            }

            return Page();
        }
    }
}
