using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class SportPositionConfiguration : IEntityTypeConfiguration<SportPosition>
{
    public void Configure(EntityTypeBuilder<SportPosition> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Code).HasMaxLength(12);
        builder.Property(e => e.NameI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());

        builder.HasIndex(e => new { e.SportId, e.Code }).IsUnique();
    }
}
