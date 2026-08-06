using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Platform).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Locale).HasMaxLength(5);

        builder.HasOne(e => e.User).WithMany(u => u.Devices).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId);
    }
}
