using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class VenuePhotoConfiguration : IEntityTypeConfiguration<VenuePhoto>
{
    public void Configure(EntityTypeBuilder<VenuePhoto> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.HasOne(e => e.Media).WithMany().HasForeignKey(e => e.MediaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.VenueId, e.MediaId }).IsUnique();
    }
}
