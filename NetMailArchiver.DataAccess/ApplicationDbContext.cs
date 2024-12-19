using Microsoft.EntityFrameworkCore;
using NetMailArchiver.Models;

namespace NetMailArchiver.DataAccess
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Email> Emails;
        public DbSet<Attachment> Attachments;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Email>()
                .Property(e => e.HtmlBody)
                .HasColumnType("text");

            modelBuilder.Entity<Attachment>()
                .Property(a => a.FileData)
                .HasColumnType("bytea");
        }
    }
}
