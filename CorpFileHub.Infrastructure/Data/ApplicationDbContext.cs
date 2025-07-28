using Microsoft.EntityFrameworkCore;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FileItem> Files { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<FileVersion> FileVersions { get; set; }
        public DbSet<AccessRule> AccessRules { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Group> Groups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}