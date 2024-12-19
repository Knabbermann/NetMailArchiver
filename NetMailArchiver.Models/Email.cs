using System.ComponentModel.DataAnnotations;

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

        public ICollection<Attachment>? Attachments { get; set; }
    }

}
