using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.ImapConfigs
{
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public AddModel(ApplicationDbContext context,
            IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        [BindProperty]
        public ImapInformation cImapInformation { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost(ImapInformation cImapInformation)
        {
            cImapInformation.Id = Guid.NewGuid();
            ModelState.Remove("cImapInformation.Id");
            if (ModelState.IsValid)
            {
                _context.ImapInformations.Add(cImapInformation);
                _context.SaveChanges();
                _toastNotification.AddSuccessToastMessage("Successfully added Mail Account.");
                return RedirectToPage("/ImapConfigs/Index");
            }

            return Page();
        }
    }
}
