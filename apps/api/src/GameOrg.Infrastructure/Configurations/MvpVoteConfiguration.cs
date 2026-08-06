using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class MvpVoteConfiguration : IEntityTypeConfiguration<MvpVote>
{
    public void Configure(EntityTypeBuilder<MvpVote> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasOne(e => e.Voter).WithMany().HasForeignKey(e => e.VoterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TargetUser).WithMany().HasForeignKey(e => e.TargetUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.EventId, e.VoterId }).IsUnique();
        builder.HasIndex(e => new { e.EventId, e.TargetUserId });
    }
}
