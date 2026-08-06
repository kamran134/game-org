using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasOne(e => e.Follower).WithMany().HasForeignKey(e => e.FollowerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetUser).WithMany().HasForeignKey(e => e.TargetUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetClub).WithMany().HasForeignKey(e => e.TargetClubId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetVenue).WithMany().HasForeignKey(e => e.TargetVenueId).OnDelete(DeleteBehavior.Cascade);

        // CHECK "ровно одна цель" и "нельзя подписаться на себя" — в 001_constraints.sql.
        builder.HasIndex(e => new { e.FollowerId, e.TargetUserId }).IsUnique();
        builder.HasIndex(e => new { e.FollowerId, e.TargetClubId }).IsUnique();
        builder.HasIndex(e => new { e.FollowerId, e.TargetVenueId }).IsUnique();
        builder.HasIndex(e => e.TargetUserId);
        builder.HasIndex(e => e.TargetClubId);
        builder.HasIndex(e => e.TargetVenueId);
    }
}
