using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.HasKey(g => g.Id);

            builder.Property(g => g.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(g => g.Description)
                .HasMaxLength(500);

            // Уникальное имя группы
            builder.HasIndex(g => g.Name)
                .IsUnique();

            // Связи
            builder.HasOne(g => g.CreatedBy)
                .WithMany()
                .HasForeignKey(g => g.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(g => g.AccessRules)
                .WithOne(ar => ar.Group)
                .HasForeignKey(ar => ar.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-many связь с пользователями настроена в UserConfiguration
        }
    }
}