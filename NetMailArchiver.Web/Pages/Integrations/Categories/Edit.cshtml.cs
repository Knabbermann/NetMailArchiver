using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;

namespace NetMailArchiver.Web.Pages.Integrations.Categories
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public EditModel(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        [BindProperty]
        public Category Category { get; set; } = new();

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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingCategory = await _context.Categories.FindAsync(Category.Id);

            if (existingCategory == null)
            {
                _toastNotification.AddErrorToastMessage("Category not found");
                return RedirectToPage("/Integrations/Categories/Index");
            }

            // Update properties
            existingCategory.Name = Category.Name;
            existingCategory.Description = Category.Description;
            existingCategory.Color = Category.Color;
            existingCategory.Icon = Category.Icon;

            // Only allow changing IsDefault if not a system category
            if (!existingCategory.IsSystem)
            {
                existingCategory.IsDefault = Category.IsDefault;
            }

            try
            {
                await _context.SaveChangesAsync();
                _toastNotification.AddSuccessToastMessage($"Category '{Category.Name}' updated successfully!");
            }
            catch (DbUpdateException)
            {
                _toastNotification.AddErrorToastMessage("Error updating category. Name might already exist.");
                return Page();
            }

            return RedirectToPage("/Integrations/Categories/Index");
        }
    }
}
