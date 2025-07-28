using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class AccessRuleConfiguration : IEntityTypeConfiguration<AccessRule>
    {
        public void Configure(EntityTypeBuilder<AccessRule> builder)
        {
            builder.HasKey(ar => ar.Id);

            // Ограничения - должен быть указан либо FileId, либо FolderId
            builder.HasCheckConstraint("CK_AccessRule_FileOrFolder",
                "(\"FileId\" IS NOT NULL AND \"FolderId\" IS NULL) OR (\"FileId\" IS NULL AND \"FolderId\" IS NOT NULL)");

            // Ограничения - должен быть указан либо UserId, либо GroupId
            builder.HasCheckConstraint("CK_AccessRule_UserOrGroup",
                "(\"UserId\" IS NOT NULL AND \"GroupId\" IS NULL) OR (\"UserId\" IS NULL AND \"GroupId\" IS NOT NULL)");

            // Индексы
            builder.HasIndex(ar => new { ar.FileId, ar.UserId });
            builder.HasIndex(ar => new { ar.FolderId, ar.UserId });
            builder.HasIndex(ar => new { ar.FileId, ar.GroupId });
            builder.HasIndex(ar => new { ar.FolderId, ar.GroupId });

            // Связи
            builder.HasOne(ar => ar.File)
                .WithMany(f => f.AccessRules)
                .HasForeignKey(ar => ar.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ar => ar.Folder)
                .WithMany(f => f.AccessRules)
                .HasForeignKey(ar => ar.FolderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ar => ar.User)
                .WithMany(u => u.AccessRules)
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ar => ar.Group)
                .WithMany(g => g.AccessRules)
                .HasForeignKey(ar => ar.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ar => ar.CreatedBy)
                .WithMany()
                .HasForeignKey(ar => ar.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}