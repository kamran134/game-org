using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        // Уникальность handle среди "живых" — partial unique index в docs/schema/001_constraints.sql,
        // не здесь: обычный @unique блокировал бы переиспользование ника после soft-delete.
        builder.Property(e => e.Handle).HasColumnType("citext");
        builder.Property(e => e.DisplayName).HasMaxLength(80);
        builder.Property(e => e.Bio).HasMaxLength(500);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Locale).HasMaxLength(5);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Gender).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.ProfileVisibility).HasConversion<string>().HasMaxLength(32);

        builder.HasOne(e => e.City)
            .WithMany()
            .HasForeignKey(e => e.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Avatar)
            .WithMany()
            .HasForeignKey(e => e.AvatarId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Accounts).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Sessions).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Sports).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Notifications).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Reliability).WithOne(e => e.User).HasForeignKey<ReliabilityStat>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ClubMemberships)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Participations)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasIndex(e => new { e.CityId, e.Status });
        builder.HasIndex(e => e.LastActiveAt).IsDescending();
    }
}
