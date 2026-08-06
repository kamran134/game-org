using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class UserSportPositionConfiguration : IEntityTypeConfiguration<UserSportPosition>
{
    public void Configure(EntityTypeBuilder<UserSportPosition> builder)
    {
        builder.HasKey(e => new { e.UserSportId, e.PositionId });
        builder.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Cascade);
    }
}
