using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class FileVersionConfiguration : IEntityTypeConfiguration<FileVersion>
    {
        public void Configure(EntityTypeBuilder<FileVersion> builder)
        {
            builder.HasKey(fv => fv.Id);

            builder.Property(fv => fv.LocalPath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(fv => fv.YandexDiskPath)
                .HasMaxLength(1000);

            builder.Property(fv => fv.Comment)
                .HasMaxLength(500);

            builder.Property(fv => fv.Hash)
                .HasMaxLength(64);

            // Индексы
            builder.HasIndex(fv => new { fv.FileId, fv.Version })
                .IsUnique();

            // Связи
            builder.HasOne(fv => fv.File)
                .WithMany(f => f.Versions)
                .HasForeignKey(fv => fv.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fv => fv.CreatedBy)
                .WithMany()
                .HasForeignKey(fv => fv.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}