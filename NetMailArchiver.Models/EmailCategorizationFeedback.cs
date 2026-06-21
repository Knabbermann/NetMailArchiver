using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetMailArchiver.Models
{
    /// <summary>
    /// Tracks email categorization feedback for learning and improvement.
    /// Stores both AI suggestions and manual corrections to enable RAG-based learning.
    /// </summary>
    public class EmailCategorizationFeedback
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Reference to the email that was categorized
        /// </summary>
        [Required]
        public Guid EmailId { get; set; }

        [ForeignKey(nameof(EmailId))]
        public virtual Email Email { get; set; } = null!;

        /// <summary>
        /// Category suggested by AI (can be null if manually categorized from start)
        /// </summary>
        public int? AiSuggestedCategoryId { get; set; }

        [ForeignKey(nameof(AiSuggestedCategoryId))]
        public virtual Category? AiSuggestedCategory { get; set; }

        /// <summary>
        /// Final category assigned (either AI suggestion or manual correction)
        /// </summary>
        [Required]
        public int FinalCategoryId { get; set; }

        [ForeignKey(nameof(FinalCategoryId))]
        public virtual Category FinalCategory { get; set; } = null!;

        /// <summary>
        /// Indicates if the user manually changed the category (correction)
        /// </summary>
        public bool WasManuallyChanged { get; set; } = false;

        /// <summary>
        /// Cached email sender for faster similarity searches
        /// </summary>
        [StringLength(500)]
        public string EmailFrom { get; set; } = string.Empty;

        /// <summary>
        /// Cached email subject for faster similarity searches
        /// </summary>
        [StringLength(1000)]
        public string EmailSubject { get; set; } = string.Empty;

        /// <summary>
        /// AI confidence score (0-100) if available
        /// </summary>
        public int? Confidence { get; set; }

        /// <summary>
        /// When this feedback was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
