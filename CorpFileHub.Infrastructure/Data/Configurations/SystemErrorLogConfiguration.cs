using CorpFileHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class SystemErrorLogConfiguration : IEntityTypeConfiguration<SystemErrorLog>
    {
        public void Configure(EntityTypeBuilder<SystemErrorLog> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Message).HasMaxLength(1000);
            builder.Property(l => l.Details).HasMaxLength(2000);
            builder.HasIndex(l => l.CreatedAt);
        }
    }
}
