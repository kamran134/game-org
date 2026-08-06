using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class EventResultConfiguration : IEntityTypeConfiguration<EventResult>
{
    public void Configure(EntityTypeBuilder<EventResult> builder)
    {
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.Summary).HasMaxLength(500);
        builder.Property(e => e.Standings).HasColumnType("jsonb").HasConversion(JsonConversions.For<List<Dictionary<string, object>>>());

        builder.HasOne(e => e.RecordedBy).WithMany().HasForeignKey(e => e.RecordedById).OnDelete(DeleteBehavior.SetNull);
    }
}
