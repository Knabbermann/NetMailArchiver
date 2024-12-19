using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NToastNotify;

namespace IIS_Manager.Web.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IToastNotification _toastNotification;

        public IndexModel(
            IToastNotification toastNotification)
        {
            _toastNotification = toastNotification;
        }

        public void OnGet()
        {
            
        }
    }
}