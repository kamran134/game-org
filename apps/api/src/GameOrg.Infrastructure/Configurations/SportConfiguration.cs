using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Slug).HasColumnType("citext");
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.Property(e => e.NameI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.Emoji).HasMaxLength(8);

        builder.HasMany(e => e.Positions).WithOne(e => e.Sport).HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Cascade);
    }
}
