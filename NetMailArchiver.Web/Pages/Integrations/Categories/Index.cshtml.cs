using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;

namespace NetMailArchiver.Web.Pages.Integrations.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Category> Categories { get; set; } = new();

        public async Task OnGetAsync()
        {
            Categories = await _context.Categories
                .OrderBy(c => c.IsSystem ? 0 : 1) // System categories first
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Update email counts
            foreach (var category in Categories)
            {
                category.EmailCount = await _context.Emails.CountAsync(e => e.CategoryId == category.Id);
            }
        }
    }
}
