using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.Method).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Provider).HasMaxLength(40);
        builder.Property(e => e.ExternalId).HasMaxLength(120);
        builder.Property(e => e.Note).HasMaxLength(300);

        builder.HasOne(e => e.Event).WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Participant).WithMany(p => p.Payments).HasForeignKey(e => e.ParticipantId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Club).WithMany().HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Payer).WithMany().HasForeignKey(e => e.PayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ConfirmedBy).WithMany().HasForeignKey(e => e.ConfirmedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.Provider, e.ExternalId }).IsUnique();
        builder.HasIndex(e => new { e.EventId, e.Status });
        builder.HasIndex(e => new { e.PayerId, e.Status });
        builder.HasIndex(e => new { e.ClubId, e.CreatedAt }).IsDescending(false, true);
    }
}
