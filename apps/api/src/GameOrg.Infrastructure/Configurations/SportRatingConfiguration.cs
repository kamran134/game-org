using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class SportRatingConfiguration : IEntityTypeConfiguration<SportRating>
{
    public void Configure(EntityTypeBuilder<SportRating> builder)
    {
        builder.HasKey(e => new { e.UserId, e.SportId });
        builder.HasOne(e => e.User).WithMany(u => u.Ratings).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Sport).WithMany().HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Cascade);

        // Лидерборды.
        builder.HasIndex(e => new { e.SportId, e.Rating }).IsDescending(false, true);
    }
}
