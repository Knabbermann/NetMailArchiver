using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using NetMailArchiver.DataAccess;
using NetMailArchiver.Models;
using OpenTelemetry.Trace;

namespace NetMailArchiver.Controllers
{
    public class ImapController(ImapInformation imapInformation, ApplicationDbContext? context = null, ILogger<ImapController>? logger = null)
    {
        private readonly ILogger<ImapController> _logger = logger ?? NullLogger<ImapController>.Instance;
        private readonly ImapClient _client = new();

        public void ConnectAndAuthenticate()
        {
            var tracer = TracerProvider.Default.GetTracer("NetMailArchiver");

            using var span = tracer.StartActiveSpan("IMAP ConnectAndAuthenticate");
            try
            {
                if (_client is { IsConnected: true, IsAuthenticated: true })
                {
                    _logger.LogInformation("IMAP bereits verbunden und authentifiziert für Host: {Host}", imapInformation.Host);
                    span.SetAttribute("status", "already connected");
                    return;
                }

                _logger.LogInformation("Verbinde mit IMAP-Server {Host}:{Port} mit SSL={UseSsl}", imapInformation.Host, imapInformation.Port, imapInformation.UseSsl);
                _client.Connect(imapInformation.Host, imapInformation.Port,
                    imapInformation.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);

                _client.Authenticate(imapInformation.Username, imapInformation.Password);
                _logger.LogInformation("Erfolgreich authentifiziert bei IMAP-Server {Host} als {User}", imapInformation.Host, imapInformation.Username);

                span.SetAttribute("status", "success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Verbinden/Authentifizieren mit IMAP");
                span.SetAttribute("status", "error");
                span.RecordException(ex);
                throw;
            }
        }

        public bool IsConnectedAndAuthenticated()
        {
            return _client is { IsConnected: true, IsAuthenticated: true };
        }

        public void GetLatestEmail()
        {
            if (!IsConnectedAndAuthenticated())
            {
                _logger.LogWarning("GetLatestEmail aufgerufen, aber IMAP ist nicht verbunden/authentifiziert.");
                return;
            }

            var inbox = _client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            _logger.LogInformation("Inbox geöffnet. Anzahl vorhandener Mails: {Count}", inbox.Count);

            if (inbox.Count == 0)
            {
                _logger.LogInformation("Inbox ist leer. Keine E-Mail zum Abrufen vorhanden.");
                return;
            }

            var cMail = inbox.GetMessage(inbox.Count - 1);
            _logger.LogInformation("Neueste E-Mail geladen. Betreff: {Subject}, Absender: {From}", cMail.Subject, string.Join(", ", cMail.From));

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
                ImapInformationId = imapInformation.Id
            };

            _logger.LogInformation("E-Mail erfolgreich verarbeitet mit {AttachmentCount} Anhängen.", email.Attachments.Count);
        }

        public async Task ArchiveNewMails(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!IsConnectedAndAuthenticated())
                throw new Exception("NotConnectedOrAuthenticated");

            if (context == null)
                throw new Exception("NoContext");

            _logger.LogInformation("Starte Archivierung neuer E-Mails für {User}@{Host}", imapInformation.Username, imapInformation.Host);

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
            inbox.Open(FolderAccess.ReadOnly);

            _logger.LogInformation("Suche nach E-Mails nach {LastDate}", lastArchivedMailDate);
            var uids = inbox.Search(SearchQuery.DeliveredAfter(lastArchivedMailDate));

            var batchSize = 10;
            var emailBatch = new List<Email>();
            int totalProcessed = 0;

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Archivierung abgebrochen.");
                    break;
                }

                var cMail = inbox.GetMessage(uid);
                _logger.LogDebug("Verarbeite Nachricht UID {UID} - Betreff: {Subject}", uid, cMail.Subject);

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
                    _logger.LogDebug("Neue E-Mail zur Batch hinzugefügt. MessageId: {MessageId}", email.MessageId);
                }

                if (emailBatch.Count >= batchSize)
                {
                    _logger.LogInformation("Speichere Batch mit {Count} E-Mails...", emailBatch.Count);
                    context.Emails.AddRange(emailBatch);
                    await context.SaveChangesAsync(cancellationToken);
                    emailBatch.Clear();
                }

                int totalProcessedPercent = (int)Math.Round((double)totalProcessed / uids.Count * 100);
                progress?.Report(totalProcessedPercent);
            }

            if (emailBatch.Any())
            {
                _logger.LogInformation("Speichere verbleibende {Count} E-Mails...", emailBatch.Count);
                context.Emails.AddRange(emailBatch);
                await context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Archivierung neuer E-Mails abgeschlossen.");
            progress?.Report(100);
        }

        public async Task ArchiveAllMails(IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (!IsConnectedAndAuthenticated())
                throw new Exception("NotConnectedOrAuthenticated");

            if (context == null)
                throw new Exception("NoContext");

            _logger.LogInformation("Starte vollständige Archivierung aller E-Mails für {User}@{Host}", imapInformation.Username, imapInformation.Host);

            var cMessageIds = await context.Emails
                .Where(e => e.ImapInformationId == imapInformation.Id)
                .Select(e => e.MessageId)
                .ToListAsync(cancellationToken);

            var inbox = _client.Inbox;
            inbox.Open(FolderAccess.ReadOnly);
            var uids = inbox.Search(SearchQuery.All);

            var batchSize = 10;
            var emailBatch = new List<Email>();
            int totalProcessed = 0;

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Archivierung abgebrochen.");
                    break;
                }

                var cMail = inbox.GetMessage(uid);
                _logger.LogDebug("Verarbeite Nachricht UID {UID} - Betreff: {Subject}", uid, cMail.Subject);

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
                    _logger.LogDebug("Neue E-Mail zur Batch hinzugefügt. MessageId: {MessageId}", email.MessageId);
                }

                if (emailBatch.Count >= batchSize)
                {
                    _logger.LogInformation("Speichere Batch mit {Count} E-Mails...", emailBatch.Count);
                    context.Emails.AddRange(emailBatch);
                    await context.SaveChangesAsync(cancellationToken);
                    emailBatch.Clear();
                }

                int totalProcessedPercent = (int)Math.Round((double)totalProcessed / uids.Count * 100);
                progress?.Report(totalProcessedPercent);
            }

            if (emailBatch.Any())
            {
                _logger.LogInformation("Speichere verbleibende {Count} E-Mails...", emailBatch.Count);
                context.Emails.AddRange(emailBatch);
                await context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Archivierung aller E-Mails abgeschlossen.");
            progress?.Report(100);
        }

        private byte[] GetAttachmentData(MimePart attachment)
        {
            using var memoryStream = new MemoryStream();
            attachment.Content.DecodeTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
