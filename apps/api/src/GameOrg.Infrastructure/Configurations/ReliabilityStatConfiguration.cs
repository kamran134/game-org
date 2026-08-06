using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class ReliabilityStatConfiguration : IEntityTypeConfiguration<ReliabilityStat>
{
    public void Configure(EntityTypeBuilder<ReliabilityStat> builder)
    {
        builder.HasKey(e => e.UserId);
        builder.HasIndex(e => e.Score);
    }
}
