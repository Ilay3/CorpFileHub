using CorpFileHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorpFileHub.Infrastructure.Data.Configurations
{
    public class LoginLogConfiguration : IEntityTypeConfiguration<LoginLog>
    {
        public void Configure(EntityTypeBuilder<LoginLog> builder)
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.IpAddress).HasMaxLength(45);
            builder.Property(l => l.UserAgent).HasMaxLength(500);
            builder.Property(l => l.ErrorMessage).HasMaxLength(1000);
            builder.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
