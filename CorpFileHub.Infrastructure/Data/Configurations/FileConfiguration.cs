﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class FileConfiguration : IEntityTypeConfiguration<FileItem>
    {
        public void Configure(EntityTypeBuilder<FileItem> builder)
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

            builder.Property(f => f.ContentType)
                .HasMaxLength(100);

            builder.Property(f => f.Extension)
                .HasMaxLength(10);

            builder.Property(f => f.Tags)
                .HasMaxLength(500);

            builder.Property(f => f.Description)
                .HasMaxLength(1000);

            // Индексы
            builder.HasIndex(f => f.Path);
            builder.HasIndex(f => f.Name);
            builder.HasIndex(f => new { f.Name, f.FolderId });

            // Связи
            builder.HasOne(f => f.Owner)
                .WithMany(u => u.OwnedFiles)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Folder)
                .WithMany(folder => folder.Files)
                .HasForeignKey(f => f.FolderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.Versions)
                .WithOne(v => v.File)
                .HasForeignKey(v => v.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.AccessRules)
                .WithOne(ar => ar.File)
                .HasForeignKey(ar => ar.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}