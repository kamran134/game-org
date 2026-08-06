using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Slug).HasColumnType("citext");
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.Property(e => e.NameI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.CountryCode).HasMaxLength(2).IsFixedLength();
        builder.HasIndex(e => new { e.CountryCode, e.IsActive });
    }
}
