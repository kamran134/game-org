using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        // Уникальность slug среди "живых" — partial unique index в 001_constraints.sql.
        builder.Property(e => e.Slug).HasColumnType("citext");
        builder.Property(e => e.Name).HasMaxLength(120);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Address).HasMaxLength(300);

        builder.Property(e => e.Location).HasColumnType("geography (Point, 4326)");
        builder.HasIndex(e => e.Location).HasMethod("GIST");

        builder.Property(e => e.Surface).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.PriceHint);
        builder.Property(e => e.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Website).HasMaxLength(200);
        builder.Property(e => e.OpeningHours).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.RatingAvg).HasPrecision(3, 2);

        builder.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Sports).WithOne(e => e.Venue).HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Photos).WithOne(e => e.Venue).HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Reviews).WithOne(e => e.Venue).HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasIndex(e => new { e.CityId, e.Status });
        builder.HasIndex(e => new { e.Status, e.RatingAvg }).IsDescending(false, true);
    }
}
