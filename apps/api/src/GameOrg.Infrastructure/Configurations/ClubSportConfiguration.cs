using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class ClubSportConfiguration : IEntityTypeConfiguration<ClubSport>
{
    public void Configure(EntityTypeBuilder<ClubSport> builder)
    {
        builder.HasKey(e => new { e.ClubId, e.SportId });
        builder.HasOne(e => e.Sport).WithMany().HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.SportId);
    }
}
