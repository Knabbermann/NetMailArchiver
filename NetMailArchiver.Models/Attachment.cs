using System.ComponentModel.DataAnnotations;

namespace NetMailArchiver.Models
{
    public class Attachment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FileName { get; set; }

        public string ContentType { get; set; }

        public long FileSize { get; set; }

        public byte[] FileData { get; set; }

        public Guid EmailId { get; set; }

        public Email Email { get; set; }
    }
}
