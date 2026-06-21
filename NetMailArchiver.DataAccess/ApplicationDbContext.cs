using Microsoft.EntityFrameworkCore;
using NetMailArchiver.Models;

namespace NetMailArchiver.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Email> Emails { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<ImapInformation> ImapInformations { get; set; }
        public DbSet<IntegrationSettings> IntegrationSettings { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Email>()
                .Property(e => e.HtmlBody)
                .HasColumnType("text");

            modelBuilder.Entity<Email>()
                .Property(e => e.TextBody)
                .HasColumnType("text");

            // Performance indexes for search
            modelBuilder.Entity<Email>()
                .HasIndex(e => e.Subject);

            modelBuilder.Entity<Email>()
                .HasIndex(e => e.From);

            // For TextBody, we'll use PostgreSQL Full-Text Search (GIN index)
            // This is configured via raw SQL migration instead of standard index
            // because TextBody is too long for B-Tree index

            modelBuilder.Entity<Email>()
                .HasIndex(e => e.Date);

            modelBuilder.Entity<Email>()
                .HasIndex(e => e.ImapInformationId);

            modelBuilder.Entity<Attachment>()
                .Property(a => a.FileData)
                .HasColumnType("bytea");

            modelBuilder.Entity<ImapInformation>();

            modelBuilder.Entity<IntegrationSettings>()
                .Property(i => i.N8nWebhookUrl)
                .HasMaxLength(500);

            modelBuilder.Entity<IntegrationSettings>()
                .Property(i => i.Description)
                .HasMaxLength(1000);

            // Category configuration
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Category>()
                .Property(c => c.Color)
                .IsRequired()
                .HasMaxLength(7);

            // Email-Category relationship
            modelBuilder.Entity<Email>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Emails)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Don't delete emails when category is deleted
        }
    }
}
