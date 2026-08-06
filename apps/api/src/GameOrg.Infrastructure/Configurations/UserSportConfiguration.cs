using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class UserSportConfiguration : IEntityTypeConfiguration<UserSport>
{
    public void Configure(EntityTypeBuilder<UserSport> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Level).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Footedness).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Note).HasMaxLength(280);
        builder.Property(e => e.Visibility).HasConversion<string>().HasMaxLength(32);

        builder.HasOne(e => e.Sport).WithMany().HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Positions).WithOne(e => e.UserSport).HasForeignKey(e => e.UserSportId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.SportId }).IsUnique();
        builder.HasIndex(e => new { e.SportId, e.Level });
    }
}
