using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.BucketKey).HasMaxLength(300);
        builder.HasIndex(e => e.BucketKey).IsUnique();
        builder.Property(e => e.MimeType).HasMaxLength(60);
        builder.Property(e => e.Blurhash).HasMaxLength(60);

        builder.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasIndex(e => e.OwnerId);
    }
}
