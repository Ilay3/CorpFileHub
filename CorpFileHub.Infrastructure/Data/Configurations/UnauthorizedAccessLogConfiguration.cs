using CorpFileHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class UnauthorizedAccessLogConfiguration : IEntityTypeConfiguration<UnauthorizedAccessLog>
    {
        public void Configure(EntityTypeBuilder<UnauthorizedAccessLog> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Action).HasMaxLength(100);
            builder.Property(l => l.EntityType).HasMaxLength(50);
            builder.Property(l => l.Description).HasMaxLength(1000);
            builder.Property(l => l.IpAddress).HasMaxLength(45);
            builder.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
