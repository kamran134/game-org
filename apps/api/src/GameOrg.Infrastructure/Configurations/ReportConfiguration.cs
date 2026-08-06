using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Reason).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Comment).HasMaxLength(1000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ResolutionNote).HasMaxLength(500);

        builder.HasOne(e => e.Reporter).WithMany().HasForeignKey(e => e.ReporterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetUser).WithMany().HasForeignKey(e => e.TargetUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetEvent).WithMany().HasForeignKey(e => e.TargetEventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetVenue).WithMany().HasForeignKey(e => e.TargetVenueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetReview).WithMany().HasForeignKey(e => e.TargetReviewId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ResolvedBy).WithMany().HasForeignKey(e => e.ResolvedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.Status, e.CreatedAt });
        builder.HasIndex(e => e.ReporterId);
    }
}
