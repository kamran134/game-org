using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Provider).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Email).HasColumnType("citext");
        builder.Property(e => e.Meta).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, object>>());

        builder.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Email);
    }
}
