using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class EventTeamConfiguration : IEntityTypeConfiguration<EventTeam>
{
    public void Configure(EntityTypeBuilder<EventTeam> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Name).HasMaxLength(40);
        builder.Property(e => e.ColorHex).HasMaxLength(7).IsFixedLength();

        builder.HasIndex(e => e.EventId);
    }
}
