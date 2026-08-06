using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.HasKey(e => new { e.BlockerId, e.BlockedId });
        builder.Property(e => e.Reason).HasMaxLength(200);

        builder.HasOne(e => e.Blocker).WithMany().HasForeignKey(e => e.BlockerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Blocked).WithMany().HasForeignKey(e => e.BlockedId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.BlockedId);
    }
}
