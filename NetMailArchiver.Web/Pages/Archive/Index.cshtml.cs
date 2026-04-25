using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using NToastNotify;
using System.IO.Compression;
using System.Text.RegularExpressions;

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

        public JsonResult OnGetMails([FromQuery] Guid ImapId, [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery = "", [FromQuery] bool searchBody = false)
        {
            if(page < 1) page = 1;

            var emailsQuery = _context.Emails.Where(x => x.ImapInformationId == ImapId);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                if (searchBody)
                {
                    // Use ILIKE for case-insensitive search on all fields
                    // ILIKE works with GIN index for TextBody
                    emailsQuery = emailsQuery.Where(e => 
                        EF.Functions.ILike(e.Subject ?? "", $"%{searchQuery}%") || 
                        EF.Functions.ILike(e.From ?? "", $"%{searchQuery}%") || 
                        EF.Functions.ILike(e.TextBody ?? "", $"%{searchQuery}%"));
                }
                else
                {
                    emailsQuery = emailsQuery.Where(e => 
                        EF.Functions.ILike(e.Subject ?? "", $"%{searchQuery}%") || 
                        EF.Functions.ILike(e.From ?? "", $"%{searchQuery}%"));
                }
            }

            // Load only necessary fields for performance
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
                    x.IsFavorite,
                    x.IsFollowUp,
                    x.TextBody,  // Use pre-processed TextBody instead of HtmlBody
                    HasAttachments = x.Attachments.Any(),
                    AttachmentCount = x.Attachments.Count()
                }).ToList();

            var emailsWithPreview = emails.Select(x => new
            {
                x.Id,
                x.Date,
                x.From,
                Subject = !string.IsNullOrWhiteSpace(searchQuery) ? HighlightSearchTerm(x.Subject, searchQuery) : x.Subject,
                x.IsFavorite,
                x.IsFollowUp,
                Preview = GetSearchPreview(x.TextBody, searchQuery, searchBody, 150),
                Attachments = new { Count = x.AttachmentCount }
            }).ToList();

            var totalEmails = emailsQuery.Count();
            var hasMorePages = (page * pageSize) < totalEmails;

            return new JsonResult(new { emails = emailsWithPreview, hasMorePages });
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

        public JsonResult OnPostToggleFavorite([FromBody] ToggleRequest request)
        {
            var email = _context.Emails.FirstOrDefault(x => x.Id == request.EmailId);
            if (email != null)
            {
                email.IsFavorite = !email.IsFavorite;
                _context.SaveChanges();
                return new JsonResult(new { isFavorite = email.IsFavorite });
            }
            return new JsonResult(new { isFavorite = false });
        }

        public JsonResult OnPostToggleFollowUp([FromBody] ToggleRequest request)
        {
            var email = _context.Emails.FirstOrDefault(x => x.Id == request.EmailId);
            if (email != null)
            {
                email.IsFollowUp = !email.IsFollowUp;
                _context.SaveChanges();
                return new JsonResult(new { isFollowUp = email.IsFollowUp });
            }
            return new JsonResult(new { isFollowUp = false });
        }

        private string GetTextPreview(string? htmlBody, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(htmlBody))
                return string.Empty;

            var text = htmlBody;

            // Remove script tags and their content
            text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);

            // Remove style tags and their content
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

            // Remove head tag and its content
            text = Regex.Replace(text, @"<head[^>]*>[\s\S]*?</head>", string.Empty, RegexOptions.IgnoreCase);

            // Remove HTML comments
            text = Regex.Replace(text, @"<!--[\s\S]*?-->", string.Empty);

            // Remove all HTML tags
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Remove extra whitespace and newlines
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // Truncate to maxLength
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength) + "...";
            }

            return text;
        }

        private string GetSearchPreview(string? textBody, string? searchQuery, bool searchBody, int maxLength = 150)
        {
            if (string.IsNullOrWhiteSpace(textBody))
                return string.Empty;

            // TextBody is already cleaned, no need to process HTML
            var text = textBody;

            // If no search query, return normal preview
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return TruncateText(text, maxLength);
            }

            // If searching in body, find and show context around the search term
            if (searchBody)
            {
                var index = text.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    // Calculate context around the found term
                    var contextBefore = 60;
                    var contextAfter = 60;

                    var start = Math.Max(0, index - contextBefore);
                    var end = Math.Min(text.Length, index + searchQuery.Length + contextAfter);
                    var length = end - start;

                    var preview = text.Substring(start, length);

                    // Add ellipsis if we're not at the start/end
                    if (start > 0) preview = "..." + preview;
                    if (end < text.Length) preview = preview + "...";

                    // Highlight the search term
                    preview = HighlightSearchTerm(preview, searchQuery);

                    return preview;
                }
            }

            // If not searching in body or term not found, return normal preview with highlighting
            var normalPreview = TruncateText(text, maxLength);

            // Still highlight the search term if it appears in the preview
            return HighlightSearchTerm(normalPreview, searchQuery);
        }

        private string CleanHtml(string? htmlBody)
        {
            if (string.IsNullOrWhiteSpace(htmlBody))
                return string.Empty;

            var text = htmlBody;

            // Remove script tags and their content
            text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);

            // Remove style tags and their content
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

            // Remove head tag and its content
            text = Regex.Replace(text, @"<head[^>]*>[\s\S]*?</head>", string.Empty, RegexOptions.IgnoreCase);

            // Remove HTML comments
            text = Regex.Replace(text, @"<!--[\s\S]*?-->", string.Empty);

            // Remove all HTML tags
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Remove extra whitespace and newlines
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        private string TruncateText(string text, int maxLength)
        {
            if (text.Length > maxLength)
            {
                return text.Substring(0, maxLength) + "...";
            }
            return text;
        }

        private string HighlightSearchTerm(string? text, string? searchQuery)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(searchQuery))
                return text ?? string.Empty;

            // Use regex for case-insensitive replacement and preserve original case
            var pattern = Regex.Escape(searchQuery);
            var highlighted = Regex.Replace(
                text, 
                pattern, 
                match => $"<mark class='search-highlight'>{match.Value}</mark>", 
                RegexOptions.IgnoreCase
            );

            return highlighted;
        }

        public class ToggleRequest
        {
            public Guid EmailId { get; set; }
        }
    }
}
