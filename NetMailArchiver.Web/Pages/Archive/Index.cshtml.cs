using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;
using System.IO.Compression;

namespace NetMailArchiver.Web.Pages.Archive
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;

        public IndexModel(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _context = context;
            _toastNotification = toastNotification;
        }

        public IEnumerable<ImapInformation> ImapInformations { get; set; }

        public void OnGet()
        {
            ImapInformations = _context.ImapInformations.ToList();
        }

        public JsonResult OnGetMails([FromQuery] Guid ImapId, [FromQuery] int page, [FromQuery] int pageSize)
        {
            if(page < 1) page = 1;

            var emailsQuery = _context.Emails.Where(x => x.ImapInformationId == ImapId);
            var emails = emailsQuery
                .OrderByDescending(x => x.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    Date = x.Date.ToString("yyyy-MM-dd HH:mm"),
                    x.From,
                    x.Subject,
                    Attachments = x.Attachments.Select(a => new { a.Id})
                }).ToList();

            var totalEmails = emailsQuery.Count();
            var hasMorePages = (page * pageSize) < totalEmails;

            return new JsonResult(new { emails, hasMorePages });
        }

        public IActionResult OnGetOpenEmail(Guid emailId)
        {
            var emailQuery = _context.Emails.Where(x => x.Id == emailId);
            var email = emailQuery
                .Select(x => new
                {
                    x.HtmlBody
                }).First();
            
            return Content(email.HtmlBody, "text/html");
        }

        public IActionResult OnGetDownloadAttachment(Guid emailId)
        {
            var emailQuery = _context.Emails.Where(x => x.Id == emailId);
            var email = emailQuery
                .Select(x => new
                {
                    x.Attachments
                }).First();

            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var attachment in email.Attachments)
                    {
                        var zipEntry = zipArchive.CreateEntry(attachment.FileName);
                        using (var entryStream = zipEntry.Open())
                        {
                            entryStream.Write(attachment.FileData, 0, attachment.FileData.Length);
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                return File(memoryStream.ToArray(), "application/zip", $"attachments_{emailId}.zip");
            }
        }
    }
}
