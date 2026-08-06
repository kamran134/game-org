using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasMaxLength(80);
        builder.Property(e => e.Endpoint).HasMaxLength(120);
        builder.Property(e => e.RequestHash).HasMaxLength(64).IsFixedLength();
        builder.Property(e => e.ResponseBody).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());

        builder.HasIndex(e => e.ExpiresAt);
    }
}
