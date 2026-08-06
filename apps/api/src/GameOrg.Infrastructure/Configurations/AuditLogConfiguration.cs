using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Action).HasMaxLength(60);
        builder.Property(e => e.EntityType).HasMaxLength(40);
        builder.Property(e => e.EntityId).HasMaxLength(64);
        builder.Property(e => e.Before).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());
        builder.Property(e => e.After).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());

        builder.HasOne(e => e.Actor).WithMany().HasForeignKey(e => e.ActorId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.CreatedAt }).IsDescending(false, false, true);
        builder.HasIndex(e => new { e.ActorId, e.CreatedAt }).IsDescending(false, true);
    }
}
