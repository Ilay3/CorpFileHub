using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class FolderConfiguration : IEntityTypeConfiguration<Folder>
    {
        public void Configure(EntityTypeBuilder<Folder> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(f => f.Path)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(f => f.YandexDiskPath)
                .HasMaxLength(1000);

            builder.Property(f => f.Description)
                .HasMaxLength(1000);

            builder.Property(f => f.Tags)
                .HasMaxLength(500);

            // Индексы
            builder.HasIndex(f => f.Path);
            builder.HasIndex(f => new { f.Name, f.ParentFolderId });

            // Связи
            builder.HasOne(f => f.Owner)
                .WithMany(u => u.OwnedFolders)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Самоссылающаяся связь для иерархии папок
            builder.HasOne(f => f.ParentFolder)
                .WithMany(f => f.SubFolders)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.Files)
                .WithOne(file => file.Folder)
                .HasForeignKey(file => file.FolderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.AccessRules)
                .WithOne(ar => ar.Folder)
                .HasForeignKey(ar => ar.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}