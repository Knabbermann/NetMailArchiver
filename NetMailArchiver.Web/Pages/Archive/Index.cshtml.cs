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

        public JsonResult OnGetMails([FromQuery] Guid ImapId, [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery = "")
        {
            if(page < 1) page = 1;

            var emailsQuery = _context.Emails.Where(x => x.ImapInformationId == ImapId);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                emailsQuery = emailsQuery.Where(e => e.Subject.Contains(searchQuery) || e.From.Contains(searchQuery));
            }

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
                    x.To,
                    x.Cc,
                    x.Bcc,
                    x.From,
                    x.Subject,
                    Date = x.Date.ToString("yyyy-MM-dd HH:mm"),
                    x.HtmlBody
                }).First();

            var fromAddress = email.From;
            var nameStartIndex = fromAddress.IndexOf("\"") + 1;
            var nameEndIndex = fromAddress.LastIndexOf("\"");
            var name = nameStartIndex > 0 && nameEndIndex > 0 ? fromAddress.Substring(nameStartIndex, nameEndIndex - nameStartIndex) : fromAddress.Split('<')[0].Trim();
            var emailAddress = fromAddress.Split('<')[1].Replace(">", "").Trim();

            var emailHtml = $@"
                <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; padding: 20px; }}
                            .email-details {{ margin-bottom: 20px; }}
                            .email-details th {{ text-align: left; padding-right: 10px; }}
                            .email-details td {{ padding-bottom: 5px; }}
                            .email-body {{ margin-top: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='email-details'>
                            <table>
                                <tr><th>From:</th><td>{name} &lt;{emailAddress}&gt;</td></tr>
                                <tr><th>To:</th><td>{email.To}</td></tr>
                                <tr><th>Cc:</th><td>{email.Cc}</td></tr>
                                <tr><th>Bcc:</th><td>{email.Bcc}</td></tr>
                                <tr><th>Subject:</th><td>{email.Subject}</td></tr>
                                <tr><th>Date:</th><td>{email.Date}</td></tr>
                            </table>
                        </div>
                        <div class='email-body'>
                            {email.HtmlBody}
                        </div>
                    </body>
                </html>";

            return Content(emailHtml, "text/html; charset=utf-8");
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
