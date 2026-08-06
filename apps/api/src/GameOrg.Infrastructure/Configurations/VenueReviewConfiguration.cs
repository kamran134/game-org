using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class VenueReviewConfiguration : IEntityTypeConfiguration<VenueReview>
{
    public void Configure(EntityTypeBuilder<VenueReview> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Text).HasMaxLength(2000);

        builder.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasIndex(e => new { e.VenueId, e.AuthorId }).IsUnique();
        builder.HasIndex(e => new { e.VenueId, e.CreatedAt }).IsDescending(false, true);
    }
}
