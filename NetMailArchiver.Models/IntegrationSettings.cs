using System.ComponentModel.DataAnnotations;

namespace NetMailArchiver.Models
{
    public class IntegrationSettings
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "n8n Webhook URL")]
        [StringLength(500)]
        public string? N8nWebhookUrl { get; set; }

        [Display(Name = "Webhook aktiviert")]
        public bool IsWebhookEnabled { get; set; }

        [Display(Name = "Letzte Aktualisierung")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Beschreibung")]
        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
