using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(al => al.Id);

            builder.Property(al => al.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(al => al.EntityName)
                .HasMaxLength(255);

            builder.Property(al => al.Description)
                .HasMaxLength(1000);

            builder.Property(al => al.IpAddress)
                .HasMaxLength(45); // Для IPv6

            builder.Property(al => al.UserAgent)
                .HasMaxLength(500);

            builder.Property(al => al.ErrorMessage)
                .HasMaxLength(2000);

            // Индексы для быстрого поиска
            builder.HasIndex(al => al.CreatedAt);
            builder.HasIndex(al => al.Action);
            builder.HasIndex(al => new { al.EntityType, al.EntityId });
            builder.HasIndex(al => al.UserId);

            // Связи
            builder.HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}