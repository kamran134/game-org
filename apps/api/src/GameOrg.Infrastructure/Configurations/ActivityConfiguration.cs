using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Verb).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Audience).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Payload).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());

        builder.HasOne(e => e.Actor).WithMany().HasForeignKey(e => e.ActorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Event).WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Club).WithMany().HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Venue).WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetUser).WithMany().HasForeignKey(e => e.TargetUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ActorId, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.Audience, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.ClubId, e.CreatedAt }).IsDescending(false, true);
    }
}
