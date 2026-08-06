using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AggregateType).HasMaxLength(40);
        builder.Property(e => e.AggregateId).HasMaxLength(64);
        builder.Property(e => e.Type).HasMaxLength(60);
        builder.Property(e => e.Payload).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());
        builder.Property(e => e.LastError).HasMaxLength(500);

        builder.HasIndex(e => new { e.ProcessedAt, e.CreatedAt });
    }
}
