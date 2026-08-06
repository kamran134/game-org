using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class VenueClaimConfiguration : IEntityTypeConfiguration<VenueClaim>
{
    public void Configure(EntityTypeBuilder<VenueClaim> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Evidence).HasMaxLength(1000);

        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.VenueId, e.UserId }).IsUnique();
    }
}
