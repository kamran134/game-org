using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.HasKey(e => new { e.UserId, e.AchievementId });
        builder.Property(e => e.Context).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());

        builder.HasOne(e => e.Achievement).WithMany(a => a.Users).HasForeignKey(e => e.AchievementId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.EarnedAt }).IsDescending(false, true);
    }
}
