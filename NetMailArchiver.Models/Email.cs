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

        public DateTime Date { get; set; }

        public string MessageId { get; set; }

        public ICollection<Attachment>? Attachments { get; set; }

        public ImapInformation ImapInformation { get; set; }

        [ForeignKey("ImapInformation")]
        public Guid ImapInformationId { get; set; }
    }

}
