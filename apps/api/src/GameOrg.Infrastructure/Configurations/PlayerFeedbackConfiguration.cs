using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class PlayerFeedbackConfiguration : IEntityTypeConfiguration<PlayerFeedback>
{
    public void Configure(EntityTypeBuilder<PlayerFeedback> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Comment).HasMaxLength(500);

        builder.HasOne(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Target).WithMany().HasForeignKey(e => e.TargetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.EventId, e.AuthorId, e.TargetId }).IsUnique();
        builder.HasIndex(e => e.TargetId);
    }
}
