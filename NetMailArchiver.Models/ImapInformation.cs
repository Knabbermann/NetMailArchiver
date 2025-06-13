using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetMailArchiver.Models
{
    public class ImapInformation
    {
        [Key]
        public Guid Id { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public bool AutoArchive { get; set; }
        public string ArchiveInterval { get; set; }

        [NotMapped]
        public int EmailCount { get; set; }
        [NotMapped]
        public int AttachmentCount { get; set; }
    }
}
