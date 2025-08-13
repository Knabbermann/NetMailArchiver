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
    public class ImapService(ArchiveLockService archiveLockService, ImapInformation imapInformation, ApplicationDbContext? context = null) : IDisposable
    {
        private readonly ImapClient _client = new();
        private bool _disposed = false;

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

            // Use HashSet for better performance on MessageId lookups
            var existingMessageIds = new HashSet<string>(
                await context.Emails
                    .Where(e => e.ImapInformationId == imapInformation.Id)
                    .Select(e => e.MessageId)
                    .ToListAsync(cancellationToken)
            );

            var lastArchivedMailDate = await context.Emails
                .Where(e => e.ImapInformationId == imapInformation.Id)
                .OrderByDescending(e => e.Date)
                .Select(e => e.Date)
                .FirstOrDefaultAsync(cancellationToken);

            var inbox = _client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            
            var uids = await inbox.SearchAsync(lastArchivedMailDate.Equals(DateTime.MinValue) ? SearchQuery.All : SearchQuery.DeliveredAfter(lastArchivedMailDate), cancellationToken);

            await ProcessEmailsInBatches(uids, existingMessageIds, progress, cancellationToken);
        }

        public async Task ArchiveAllMails(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!IsConnectedAndAuthenticated())
                throw new Exception("NotConnectedOrAuthenticated");

            if (context == null)
                throw new Exception("NoContext");

            // Use HashSet for better performance on MessageId lookups
            var existingMessageIds = new HashSet<string>(
                await context.Emails
                    .Where(e => e.ImapInformationId == imapInformation.Id)
                    .Select(e => e.MessageId)
                    .ToListAsync(cancellationToken)
            );

            var inbox = _client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            var uids = await inbox.SearchAsync(SearchQuery.All, cancellationToken);

            await ProcessEmailsInBatches(uids, existingMessageIds, progress, cancellationToken);
        }

        private async Task ProcessEmailsInBatches(IList<UniqueId> uids, HashSet<string> existingMessageIds, IProgress<int> progress, CancellationToken cancellationToken)
        {
            const int batchSize = 5; // Reduced batch size for better memory management
            const int gcCollectionInterval = 50; // Force GC every 50 processed emails
            
            var emailBatch = new List<Email>();
            var totalProcessed = 0;
            var totalCount = uids.Count;

            for (int i = 0; i < totalCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var uid = uids[i];
                MimeMessage? cMail = null;

                try
                {
                    // Get the message
                    cMail = await _client.Inbox.GetMessageAsync(uid, cancellationToken);

                    // Skip if we already have this message
                    if (string.IsNullOrEmpty(cMail.MessageId) || existingMessageIds.Contains(cMail.MessageId))
                    {
                        continue;
                    }

                    var email = CreateEmailFromMimeMessage(cMail);
                    emailBatch.Add(email);

                    // Save batch when it reaches the batch size
                    if (emailBatch.Count >= batchSize)
                    {
                        await SaveEmailBatch(emailBatch, cancellationToken);
                        emailBatch.Clear();
                    }
                }
                finally
                {
                    // Explicitly dispose the MimeMessage to free memory immediately
                    cMail?.Dispose();
                    cMail = null;
                }

                totalProcessed++;

                // Force garbage collection periodically to free memory
                if (totalProcessed % gcCollectionInterval == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                // Report progress
                var progressPercent = (int)Math.Round((double)totalProcessed / totalCount * 100);
                progress?.Report(progressPercent);
            }

            // Save any remaining emails in the batch
            if (emailBatch.Any())
            {
                await SaveEmailBatch(emailBatch, cancellationToken);
                emailBatch.Clear();
            }

            // Final cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            progress?.Report(100);
        }

        private Email CreateEmailFromMimeMessage(MimeMessage cMail)
        {
            var email = new Email
            {
                Subject = cMail.Subject,
                From = string.Join(", ", cMail.From.Select(f => f.ToString())),
                To = string.Join(", ", cMail.To.Select(t => t.ToString())),
                Cc = cMail.Cc?.Any() == true ? string.Join(", ", cMail.Cc.Select(c => c.ToString())) : null,
                Bcc = cMail.Bcc?.Any() == true ? string.Join(", ", cMail.Bcc.Select(b => b.ToString())) : null,
                HtmlBody = cMail.HtmlBody,
                Date = cMail.Date.UtcDateTime,
                MessageId = cMail.MessageId,
                ImapInformationId = imapInformation.Id
            };

            // Process attachments separately to minimize memory footprint
            var attachments = new List<Attachment>();
            foreach (var attachment in cMail.Attachments.OfType<MimePart>())
            {
                try
                {
                    var attachmentData = GetAttachmentData(attachment);
                    var attachmentModel = new Attachment
                    {
                        FileName = attachment.FileName ?? "unknown",
                        ContentType = attachment.ContentType?.MimeType ?? "application/octet-stream",
                        FileSize = attachment.ContentDisposition?.Size ?? attachmentData.Length,
                        FileData = attachmentData,
                        EmailId = email.Id
                    };
                    attachments.Add(attachmentModel);
                }
                catch (Exception)
                {
                    // Skip problematic attachments rather than failing the entire email
                    continue;
                }
            }

            email.Attachments = attachments;
            return email;
        }

        private async Task SaveEmailBatch(List<Email> emailBatch, CancellationToken cancellationToken)
        {
            if (context == null || !emailBatch.Any()) 
                return;

            try
            {
                context.Emails.AddRange(emailBatch);
                await context.SaveChangesAsync(cancellationToken);
                
                // Detach entities from context to free memory
                foreach (var email in emailBatch)
                {
                    context.Entry(email).State = EntityState.Detached;
                    if (email.Attachments != null)
                    {
                        foreach (var attachment in email.Attachments)
                        {
                            context.Entry(attachment).State = EntityState.Detached;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If batch save fails, try to save individual emails
                foreach (var email in emailBatch)
                {
                    try
                    {
                        context.Emails.Add(email);
                        await context.SaveChangesAsync(cancellationToken);
                        context.Entry(email).State = EntityState.Detached;
                        if (email.Attachments != null)
                        {
                            foreach (var attachment in email.Attachments)
                            {
                                context.Entry(attachment).State = EntityState.Detached;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Skip problematic emails
                        context.Entry(email).State = EntityState.Detached;
                        continue;
                    }
                }
            }
        }

        private static byte[] GetAttachmentData(MimePart attachment)
        {
            using var memoryStream = new MemoryStream();
            attachment.Content.DecodeTo(memoryStream);
            return memoryStream.ToArray();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _client?.Dispose();
                _disposed = true;
            }
        }
    }
}
