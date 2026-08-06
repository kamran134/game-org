using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class EventSeriesConfiguration : IEntityTypeConfiguration<EventSeries>
{
    public void Configure(EntityTypeBuilder<EventSeries> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.StartTime).HasMaxLength(5);
        builder.Property(e => e.Template).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());

        builder.HasOne(e => e.Club).WithMany().HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Sport).WithMany().HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Venue).WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Events).WithOne(e => e.Series).HasForeignKey(e => e.SeriesId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.IsActive, e.MaterializedUntil });
    }
}
