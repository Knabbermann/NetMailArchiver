using System.ComponentModel.DataAnnotations;

namespace NetMailArchiver.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [StringLength(7)]
        [Display(Name = "Color")]
        public string Color { get; set; } = "#6c757d"; // Default gray

        [StringLength(50)]
        [Display(Name = "Icon")]
        public string Icon { get; set; } = "fa-folder"; // Default folder icon

        [Display(Name = "Is System Category")]
        public bool IsSystem { get; set; } = false; // System categories can't be deleted

        [Display(Name = "Is Default")]
        public bool IsDefault { get; set; } = false; // Default category for uncategorized emails

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Email Count")]
        public int EmailCount { get; set; } = 0; // Computed property

        // Navigation property
        public virtual ICollection<Email> Emails { get; set; } = new List<Email>();
    }
}
