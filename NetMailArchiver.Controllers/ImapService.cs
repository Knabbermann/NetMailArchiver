using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;

namespace NetMailArchiver.Services
{
    public class ImapService(ArchiveLockService archiveLockService, ImapInformation imapInformation, ApplicationDbContext? context = null)
    {
        private readonly ImapClient _client = new();

        public void ConnectAndAuthenticate()
        {
            try
            {
                if (_client is { IsConnected: true, IsAuthenticated: true })
                {
                    return;
                }

                _client.Connect(imapInformation.Host, imapInformation.Port,
                    imapInformation.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

                _client.Authenticate(imapInformation.Username, imapInformation.Password);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool IsConnectedAndAuthenticated()
        {
            return _client is { IsConnected: true, IsAuthenticated: true };
        }

        public async Task ArchiveNewMails(IProgress<int> progress, CancellationToken cancellationToken)
        {
            using var _ = await archiveLockService.AcquireLockAsync(imapInformation.Id, cancellationToken);
            
            if (!IsConnectedAndAuthenticated())
                throw new Exception("NotConnectedOrAuthenticated");

            if (context == null)
                throw new Exception("NoContext");

            var cMessageIds = await context.Emails
                .Where(e => e.ImapInformationId == imapInformation.Id)
                .Select(e => e.MessageId)
                .ToListAsync(cancellationToken);

            var lastArchivedMailDate = await context.Emails
                .Where(e => e.ImapInformationId == imapInformation.Id)
                .OrderByDescending(e => e.Date)
                .Select(e => e.Date)
                .FirstOrDefaultAsync(cancellationToken);

            var inbox = _client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            
            var uids = await inbox.SearchAsync(lastArchivedMailDate.Equals(DateTime.MinValue) ? SearchQuery.All : SearchQuery.DeliveredAfter(lastArchivedMailDate), cancellationToken);

            const int batchSize = 10;
            var emailBatch = new List<Email>();
            var totalProcessed = 0;

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var cMail = await inbox.GetMessageAsync(uid, cancellationToken);

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
                    ImapInformationId = imapInformation.Id
                };

                totalProcessed++;

                if (!cMessageIds.Contains(email.MessageId) && email.MessageId != null)
                {
                    emailBatch.Add(email);
                }

                if (emailBatch.Count >= batchSize)
                {
                    context.Emails.AddRange(emailBatch);
                    await context.SaveChangesAsync(cancellationToken);
                    emailBatch.Clear();
                }

                var totalProcessedPercent = (int)Math.Round((double)totalProcessed / uids.Count * 100);
                progress?.Report(totalProcessedPercent);
            }

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
                .Where(e => e.ImapInformationId == imapInformation.Id)
                .Select(e => e.MessageId)
                .ToListAsync(cancellationToken);

            var inbox = _client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            var uids = await inbox.SearchAsync(SearchQuery.All, cancellationToken);

            const int batchSize = 10;
            var emailBatch = new List<Email>();
            var totalProcessed = 0;

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var cMail = await inbox.GetMessageAsync(uid, cancellationToken);

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
                    ImapInformationId = imapInformation.Id
                };

                totalProcessed++;

                if (!cMessageIds.Contains(email.MessageId) && email.MessageId != null)
                {
                    emailBatch.Add(email);
                }

                if (emailBatch.Count >= batchSize)
                {
                    context.Emails.AddRange(emailBatch);
                    await context.SaveChangesAsync(cancellationToken);
                    emailBatch.Clear();
                }

                var totalProcessedPercent = (int)Math.Round((double)totalProcessed / uids.Count * 100);
                progress?.Report(totalProcessedPercent);
            }

            if (emailBatch.Any())
            {
                context.Emails.AddRange(emailBatch);
                await context.SaveChangesAsync(cancellationToken);
            }

            progress?.Report(100);
        }

        private static byte[] GetAttachmentData(MimePart attachment)
        {
            using var memoryStream = new MemoryStream();
            attachment.Content.DecodeTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
