using GameOrg.Domain.Entities;
using GameOrg.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameOrg.Infrastructure.Configurations;

public sealed class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        // Уникальность slug среди "живых" — partial unique index в 001_constraints.sql.
        builder.Property(e => e.Slug).HasColumnType("citext");
        // Лимиты длины (80/1000 на каждый язык) — в валидации сервиса (см.
        // ClubService), не здесь: это jsonb-словарь, а не одна строка.
        builder.Property(e => e.NameI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        builder.Property(e => e.DescriptionI18n).HasColumnType("jsonb").HasConversion(JsonConversions.For<Dictionary<string, string>>());
        // Generated-колонка Postgres (см. миграцию ClubMultilingualContent) —
        // EF только читает значение обратно после save, никогда не пишет его.
        builder.Property(e => e.SearchText).HasColumnType("text").ValueGeneratedOnAddOrUpdate();
        builder.Property(e => e.Visibility).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.InviteCode).HasMaxLength(16);

        builder.HasIndex(e => e.TelegramChatId).IsUnique();
        builder.HasIndex(e => e.InviteCode).IsUnique();

        builder.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Avatar).WithMany().HasForeignKey(e => e.AvatarId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Cover).WithMany().HasForeignKey(e => e.CoverId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedById).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Sports).WithOne(e => e.Club).HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Members).WithOne(e => e.Club).HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasIndex(e => new { e.CityId, e.Visibility });
    }
}
