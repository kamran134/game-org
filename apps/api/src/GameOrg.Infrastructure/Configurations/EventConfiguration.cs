using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.PublicId).HasMaxLength(12);
        builder.HasIndex(e => e.PublicId).IsUnique();

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Visibility).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.CustomLocation).HasMaxLength(200);
        // Лимиты длины (120/2000 на каждый язык) — в валидации сервиса
        // (EventService), не здесь: это jsonb-словарь, а не одна строка.
        builder.Property(e => e.TitleI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.DescriptionI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.SkillLevelMin).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.SkillLevelMax).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.GenderPolicy).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.CostSplit).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.CancelReason).HasMaxLength(300);

        builder.HasOne(e => e.Sport).WithMany().HasForeignKey(e => e.SportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Club).WithMany().HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Venue).WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Participants).WithOne(e => e.Event).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Teams).WithOne(e => e.Event).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Result).WithOne(e => e.Event).HasForeignKey<EventResult>(e => e.EventId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Главный индекс дискавери: «публичные игры по футболу в ближайшие дни».
        builder.HasIndex(e => new { e.Visibility, e.Status, e.StartsAt });
        builder.HasIndex(e => new { e.SportId, e.StartsAt });
        builder.HasIndex(e => new { e.VenueId, e.StartsAt });
        builder.HasIndex(e => new { e.ClubId, e.StartsAt }).IsDescending(false, true);
        builder.HasIndex(e => new { e.Status, e.StartsAt });
    }
}
