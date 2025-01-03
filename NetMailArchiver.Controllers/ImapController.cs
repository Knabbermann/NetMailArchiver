using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;

namespace NetMailArchiver.Controllers
{
    public class ImapController(ImapInformation imapInfomation, ApplicationDbContext? context = null)
    {
        private ImapClient _client = new ImapClient();

        public void ConnectAndAuthenticate()
        {
            if (_client.IsConnected && _client.IsAuthenticated) return;
            
            try
            {
                _client.Connect(imapInfomation.Host, imapInfomation.Port, imapInfomation.UseSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);
                _client.Authenticate(imapInfomation.Username, imapInfomation.Password);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool IsConnectedAndAuthenticated()
        {
            if (_client.IsConnected && _client.IsAuthenticated) return true;
            return false;
        }

        public void GetLatestEmail()
        {
            if (!IsConnectedAndAuthenticated())
                return;

            var inbox = _client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);

            if (inbox.Count == 0)
                return;

            var cMail = inbox.GetMessage(inbox.Count - 1);
            var email = new Email
            {
                Subject = cMail.Subject,
                From = string.Join(", ", cMail.From.Select(f => f.ToString())),
                To = string.Join(", ", cMail.To.Select(t => t.ToString())),
                Cc = cMail.Cc != null ? string.Join(", ", cMail.Cc.Select(c => c.ToString())) : null,
                Bcc = cMail.Bcc != null ? string.Join(", ", cMail.Bcc.Select(b => b.ToString())) : null,
                HtmlBody = cMail.HtmlBody,
                Date = cMail.Date.DateTime,
                MessageId = cMail.MessageId,
                Attachments = cMail.Attachments.OfType<MimePart>().Select(a => new Attachment
                {
                    FileName = a.FileName,
                    ContentType = a.ContentType.MimeType,
                    FileSize = a.ContentDisposition?.Size ?? 0,
                    FileData = GetAttachmentData(a),
                }).ToList(),
                ImapInformationId = imapInfomation.Id
            };
        }

        public Task ArchiveMailsLast30Days()
        {
            if (!IsConnectedAndAuthenticated())
                return Task.FromException(new Exception("NotConnectedOrAuthenticated"));
            if (context == null)
                return Task.FromException(new Exception("NoContext"));

            var cMessageIds = context.Emails.Where(e => e.ImapInformationId == imapInfomation.Id).Select(e => e.MessageId).ToList();

            var inbox = _client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            var uids = inbox.Search(SearchQuery.DeliveredAfter(DateTime.Today.AddDays(-30)));
            foreach (var uid in uids)
            {
                var cMail = inbox.GetMessage(uid);
                var email = new Email
                {
                    Subject = cMail.Subject,
                    From = string.Join(", ", cMail.From.Select(f => f.ToString())),
                    To = string.Join(", ", cMail.To.Select(t => t.ToString())),
                    Cc = cMail.Cc != null ? string.Join(", ", cMail.Cc.Select(c => c.ToString())) : null,
                    Bcc = cMail.Bcc != null ? string.Join(", ", cMail.Bcc.Select(b => b.ToString())) : null,
                    HtmlBody = cMail.HtmlBody,
                    Date = cMail.Date.UtcDateTime,
                    MessageId = cMail.MessageId,
                    Attachments = cMail.Attachments.OfType<MimePart>().Select(a => new Attachment
                    {
                        FileName = a.FileName,
                        ContentType = a.ContentType.MimeType,
                        FileSize = a.ContentDisposition?.Size ?? 0,
                        FileData = GetAttachmentData(a),
                    }).ToList(),
                    ImapInformationId = imapInfomation.Id
                };
                if(!cMessageIds.Contains(email.MessageId))
                    context.Emails.Add(email);
            }
            context.SaveChanges();
            return Task.CompletedTask;
        }

        private byte[] GetAttachmentData(MimePart attachment)
        {
            using (var memoryStream = new MemoryStream())
            {
                attachment.Content.DecodeTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
