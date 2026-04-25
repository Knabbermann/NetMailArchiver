using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetMailArchiver.Models
{
    public class Email
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Subject { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string? Cc { get; set; }
        public string? Bcc { get; set; }

        public string? HtmlBody { get; set; }

        public string? TextBody { get; set; }  // NEW: Pre-processed plain text for fast searching

        public DateTime Date { get; set; }

        public string MessageId { get; set; }

        public bool IsFavorite { get; set; } = false;

        public bool IsFollowUp { get; set; } = false;

        public ICollection<Attachment>? Attachments { get; set; }

        public ImapInformation ImapInformation { get; set; }

        [ForeignKey("ImapInformation")]
        public Guid ImapInformationId { get; set; }
    }

}
