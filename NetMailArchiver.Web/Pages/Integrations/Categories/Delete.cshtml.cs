using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.Integrations.Categories
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public DeleteModel(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        [BindProperty]
        public Category Category { get; set; } = new();

        public int EmailCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _toastNotification.AddErrorToastMessage("Category ID is required");
                return RedirectToPage("/Integrations/Categories/Index");
            }

            Category = await _context.Categories.FindAsync(id);

            if (Category == null)
            {
                _toastNotification.AddErrorToastMessage("Category not found");
                return RedirectToPage("/Integrations/Categories/Index");
            }

            if (Category.IsSystem)
            {
                _toastNotification.AddWarningToastMessage("System categories cannot be deleted");
                return RedirectToPage("/Integrations/Categories/Index");
            }

            EmailCount = await _context.Emails.CountAsync(e => e.CategoryId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var category = await _context.Categories.FindAsync(Category.Id);

            if (category == null)
            {
                _toastNotification.AddErrorToastMessage("Category not found");
                return RedirectToPage("/Integrations/Categories/Index");
            }

            if (category.IsSystem)
            {
                _toastNotification.AddWarningToastMessage("System categories cannot be deleted");
                return RedirectToPage("/Integrations/Categories/Index");
            }

            // Get default category to reassign emails
            var defaultCategory = await _context.Categories.FirstOrDefaultAsync(c => c.IsDefault);

            // Reassign all emails from this category to default (or null)
            var emailsToReassign = await _context.Emails
                .Where(e => e.CategoryId == category.Id)
                .ToListAsync();

            foreach (var email in emailsToReassign)
            {
                email.CategoryId = defaultCategory?.Id;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _toastNotification.AddSuccessToastMessage(
                $"Category '{category.Name}' deleted. {emailsToReassign.Count} emails reassigned.");

            return RedirectToPage("/Integrations/Categories/Index");
        }
    }
}
