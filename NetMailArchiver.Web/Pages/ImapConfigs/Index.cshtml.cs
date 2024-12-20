using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;

namespace NetMailArchiver.Web.Pages.ImapConfigs
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<ImapInformation> ImapInformations { get; set; }

        public void OnGet()
        {
            ImapInformations = _context.ImapInformations.ToList();
        }
    }
}
