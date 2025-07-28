using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.Department)
                .HasMaxLength(100);

            builder.Property(u => u.Position)
                .HasMaxLength(100);

            // Уникальный индекс для Email
            builder.HasIndex(u => u.Email)
                .IsUnique();

            // Связи
            builder.HasMany(u => u.OwnedFiles)
                .WithOne(f => f.Owner)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.OwnedFolders)
                .WithOne(f => f.Owner)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь для AuditLogs - один пользователь может иметь много записей аудита
            builder.HasMany(u => u.AuditLogs)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many связь с группами
            builder.HasMany(u => u.Groups)
                .WithMany(g => g.Users)
                .UsingEntity(j => j.ToTable("UserGroups"));
        }
    }
}