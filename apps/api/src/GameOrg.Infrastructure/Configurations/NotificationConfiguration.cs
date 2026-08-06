using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Body).HasMaxLength(1000);
        builder.Property(e => e.Data).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());
        builder.Property(e => e.DedupeKey).HasMaxLength(160);
        builder.Property(e => e.Error).HasMaxLength(500);

        builder.HasIndex(e => new { e.DedupeKey, e.Channel }).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.UserId, e.ReadAt });
        builder.HasIndex(e => new { e.Status, e.ScheduledFor });
    }
}
