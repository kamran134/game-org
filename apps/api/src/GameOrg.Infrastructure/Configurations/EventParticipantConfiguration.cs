using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.GuestName).HasMaxLength(80);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Note).HasMaxLength(200);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Participations)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.InvitedBy).WithMany().HasForeignKey(e => e.InvitedById).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.EventId, e.UserId }).IsUnique();
        builder.HasIndex(e => new { e.EventId, e.Status });
        builder.HasIndex(e => new { e.UserId, e.Status });
        builder.HasIndex(e => new { e.EventId, e.WaitlistOrder });
    }
}
