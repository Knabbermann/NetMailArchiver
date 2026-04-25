using Microsoft.EntityFrameworkCore;
using NetMailArchiver.Models;

namespace NetMailArchiver.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Email> Emails { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<ImapInformation> ImapInformations { get; set; }

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
        }
    }
}
