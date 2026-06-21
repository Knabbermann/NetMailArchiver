using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.Integrations.Categories
{
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public AddModel(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        [BindProperty]
        public Category Category { get; set; } = new();

        public void OnGet()
        {
            // Set defaults
            Category.Color = "#6c757d";
            Category.Icon = "fa-folder";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Category.CreatedAt = DateTime.UtcNow;
            Category.IsSystem = false; // User-created categories are never system categories

            _context.Categories.Add(Category);
            await _context.SaveChangesAsync();

            _toastNotification.AddSuccessToastMessage($"Category '{Category.Name}' created successfully!");
            return RedirectToPage("/Integrations/Categories/Index");
        }
    }
}
