using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(40);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.Property(e => e.NameI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.DescI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.Icon).HasMaxLength(40);
    }
}
