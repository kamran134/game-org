using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class ClubMemberConfiguration : IEntityTypeConfiguration<ClubMember>
{
    public void Configure(EntityTypeBuilder<ClubMember> builder)
    {
        builder.HasKey(e => new { e.ClubId, e.UserId });
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasOne(e => e.User)
            .WithMany(u => u.ClubMemberships)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.InvitedBy)
            .WithMany()
            .HasForeignKey(e => e.InvitedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.UserId, e.Status });
        builder.HasIndex(e => new { e.ClubId, e.Role });
    }
}
