using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
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

        public async Task ArchiveNewMails(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!IsConnectedAndAuthenticated())
                throw new Exception("NotConnectedOrAuthenticated");
            if (context == null)
                throw new Exception("NoContext");
            var cMessageIds = await context.Emails
                .Where(e => e.ImapInformationId == imapInfomation.Id)
                .Select(e => e.MessageId)
                .ToListAsync(cancellationToken);

            var lastArchivedMailDate = await context.Emails
                .Where(e => e.ImapInformationId == imapInfomation.Id)
                .OrderByDescending(e => e.Date)
                .Select(e => e.Date)
                .FirstOrDefaultAsync(cancellationToken);

            var inbox = _client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);

            var uids = inbox.Search(SearchQuery.DeliveredAfter(lastArchivedMailDate));

            var batchSize = 10;
            var emailBatch = new List<Email>();
            int totalProcessed = 0;
            int totalProcessedPercent = 0;
            int totalNew = 0;

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
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

                totalProcessed++;

                if (!cMessageIds.Contains(email.MessageId) && email.MessageId != null)
                {
                    emailBatch.Add(email);
                    totalNew++;
                }

                // Wenn die Batch-Größe erreicht ist, speichern und Fortschritt melden
                if (emailBatch.Count >= batchSize)
                {
                    context.Emails.AddRange(emailBatch);
                    await context.SaveChangesAsync(cancellationToken);
                    emailBatch.Clear();
                }
                totalProcessedPercent = (int)Math.Round((double)totalProcessed / uids.Count * 100);
                progress?.Report(totalProcessedPercent);
            }

            // Reste speichern
            if (emailBatch.Any())
            {
                context.Emails.AddRange(emailBatch);
                await context.SaveChangesAsync(cancellationToken);
            }

            progress?.Report(100);
        }

        public async Task ArchiveAllMails(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!IsConnectedAndAuthenticated())
                throw new Exception("NotConnectedOrAuthenticated");
            if (context == null)
                throw new Exception("NoContext");

            var cMessageIds = await context.Emails
                .Where(e => e.ImapInformationId == imapInfomation.Id)
                .Select(e => e.MessageId)
                .ToListAsync(cancellationToken);

            var inbox = _client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            var uids = inbox.Search(SearchQuery.All);

            var batchSize = 10;
            var emailBatch = new List<Email>();
            int totalProcessed = 0;
            int totalProcessedPercent = 0;
            int totalNew = 0;

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

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

                totalProcessed++;

                if (!cMessageIds.Contains(email.MessageId) && email.MessageId != null)
                {
                    emailBatch.Add(email);
                    totalNew++;
                }

                // Wenn die Batch-Größe erreicht ist, speichern und Fortschritt melden
                if (emailBatch.Count >= batchSize)
                {
                    context.Emails.AddRange(emailBatch);
                    await context.SaveChangesAsync(cancellationToken);
                    emailBatch.Clear();
                }

                totalProcessedPercent = (int)Math.Round((double)totalProcessed / uids.Count * 100);
                // Abschlussfortschritt melden
                progress?.Report(totalProcessedPercent);
            }

            // Reste speichern
            if (emailBatch.Any())
            {
                context.Emails.AddRange(emailBatch);
                await context.SaveChangesAsync(cancellationToken);
            }

            progress?.Report(100);
        }

        private void Archive(List<string>? cMessageIds, IList<UniqueId> uids)
        { 
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
