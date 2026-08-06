using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class VenueSportConfiguration : IEntityTypeConfiguration<VenueSport>
{
    public void Configure(EntityTypeBuilder<VenueSport> builder)
    {
        builder.HasKey(e => new { e.VenueId, e.SportId });
        builder.HasOne(e => e.Sport).WithMany().HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.SportId);
    }
}
