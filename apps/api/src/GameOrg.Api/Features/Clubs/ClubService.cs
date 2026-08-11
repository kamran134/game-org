using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Clubs;

/// <summary>CRUD клубов и управление участниками.</summary>
public sealed class ClubService(GameOrgDbContext db)
{
    public async Task<(Club? Result, string? Error)> CreateAsync(Guid userId, CreateClubRequest request, CancellationToken ct)
    {
        if (request.Name.IsEmpty) return (null, "Название: хотя бы один язык обязателен.");
        if (request.Name.ExceedsMaxLength(80)) return (null, "Название — до 80 символов на каждый язык.");
        if (request.Description?.ExceedsMaxLength(1000) == true) return (null, "Описание — до 1000 символов на каждый язык.");

        var nameI18n = request.Name.ToDict()!; // не null — IsEmpty уже проверили выше
        var club = new Club
        {
            Slug = await GenerateUniqueSlugAsync(Localized.Resolve(nameI18n, RequestLocale.Default) ?? "", ct),
            NameI18n = nameI18n,
            DescriptionI18n = request.Description?.ToDict(),
            CityId = request.CityId,
            Visibility = request.Visibility ?? ClubVisibility.Public,
            CreatedById = userId,
            MembersCount = 1,
        };

        if (request.SportIds is { Count: > 0 })
        {
            foreach (var sportId in request.SportIds.Distinct())
                club.Sports.Add(new ClubSport { SportId = sportId });
        }

        // Создатель сразу становится Owner — та же транзакция, что создание клуба.
        club.Members.Add(new ClubMember { UserId = userId, Role = ClubRole.Owner, Status = MembershipStatus.Active });

        db.Clubs.Add(club);
        await db.SaveChangesAsync(ct);
        return (club, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid clubId, Guid userId, UpdateClubRequest request, CancellationToken ct)
    {
        var club = await db.Clubs.Include(c => c.Sports).FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (false, "Клуб не найден.");
        if (!await IsOwnerOrAdminAsync(clubId, userId, ct)) return (false, "Редактировать может только владелец или админ клуба.");

        // Слаг не трогаем — ссылки на клуб не должны ломаться.
        if (request.Name is not null)
        {
            if (request.Name.ExceedsMaxLength(80)) return (false, "Название — до 80 символов на каждый язык.");
            var nameDict = request.Name.ToDict();
            if (nameDict is null) return (false, "Название: хотя бы один язык обязателен.");
            club.NameI18n = nameDict;
        }
        if (request.Description is not null)
        {
            if (request.Description.ExceedsMaxLength(1000)) return (false, "Описание — до 1000 символов на каждый язык.");
            club.DescriptionI18n = request.Description.ToDict();
        }
        if (request.CityId is not null) club.CityId = request.CityId;
        if (request.Visibility is not null) club.Visibility = request.Visibility.Value;

        if (request.SportIds is not null)
        {
            club.Sports.Clear();
            foreach (var sportId in request.SportIds.Distinct())
                club.Sports.Add(new ClubSport { SportId = sportId, ClubId = club.Id });
        }

        club.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Private невиден не-участникам — 404, не 403 (не подтверждаем сам факт существования).
    /// Public/RequestOnly видны всем, разница только в том, как в них вступить.
    /// </summary>
    public async Task<ClubDetailDto?> GetBySlugAsync(string slug, string locale, Guid? viewerId, CancellationToken ct)
    {
        var club = await db.Clubs
            .Include(c => c.City)
            .Include(c => c.Sports).ThenInclude(s => s.Sport)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

        if (club is null) return null;

        ClubMember? viewerMembership = viewerId is null
            ? null
            : await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == club.Id && m.UserId == viewerId, ct);

        var viewerIsActive = viewerMembership is { Status: MembershipStatus.Active };
        if (club.Visibility == ClubVisibility.Private && !viewerIsActive) return null;

        return MapDetail(club, locale, viewerMembership);
    }

    public async Task<List<ClubDto>> GetListAsync(string locale, Guid? viewerId, Guid? cityId, Guid? sportId, CancellationToken ct)
    {
        // Private — только клубы, где viewer активный участник; остальным как будто их нет в каталоге.
        var viewerClubIds = viewerId is null
            ? []
            : await db.ClubMembers
                .Where(m => m.UserId == viewerId && m.Status == MembershipStatus.Active)
                .Select(m => m.ClubId)
                .ToListAsync(ct);

        var query = db.Clubs.Where(c => c.Visibility != ClubVisibility.Private || viewerClubIds.Contains(c.Id));
        if (cityId is not null) query = query.Where(c => c.CityId == cityId);
        if (sportId is not null) query = query.Where(c => c.Sports.Any(s => s.SportId == sportId));

        var clubs = await query
            .Include(c => c.City)
            .OrderByDescending(c => c.MembersCount)
            .Take(50)
            .ToListAsync(ct);

        return clubs.Select(c => new ClubDto(
            c.Id, c.Slug, Localized.Resolve(c.NameI18n, locale) ?? "",
            MapCity(c.City), c.Visibility, c.MembersCount, c.EventsCount)).ToList();
    }

    /// <summary>Только Owner. Soft-delete — DeletedAt, глобальный HasQueryFilter уже прячет клуб из всех выдач.</summary>
    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club is null) return (false, "Клуб не найден.");

        var membership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        if (membership is not { Role: ClubRole.Owner, Status: MembershipStatus.Active })
            return (false, "Удалить клуб может только владелец.");

        club.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    internal async Task<bool> IsOwnerOrAdminAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var membership = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        return membership is { Status: MembershipStatus.Active } && membership.Role is ClubRole.Owner or ClubRole.Admin;
    }

    private ClubDetailDto MapDetail(Club club, string locale, ClubMember? viewerMembership)
    {
        var viewerIsManager = viewerMembership is { Status: MembershipStatus.Active } && viewerMembership.Role is ClubRole.Owner or ClubRole.Admin;

        return new ClubDetailDto(
            club.Id, club.Slug,
            Localized.Resolve(club.NameI18n, locale) ?? "",
            Localized.Resolve(club.DescriptionI18n, locale),
            MapCity(club.City), club.Visibility, club.MembersCount, club.EventsCount, club.CreatedById,
            viewerIsManager ? club.InviteCode : null,
            viewerMembership?.Role, viewerMembership?.Status,
            club.Sports.Select(s => new SportDto(s.Sport.Id, s.Sport.Slug, s.Sport.NameI18n, s.Sport.Emoji, s.Sport.HasPositions, s.Sport.IsTeamSport)).ToList(),
            LocalizedTextDto.From(club.NameI18n),
            LocalizedTextDto.FromNullable(club.DescriptionI18n));
    }

    private static CityDto? MapCity(City? city) => city is null ? null : new CityDto(city.Id, city.Slug, city.NameI18n, city.Lat, city.Lng);

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = ClubSlugGenerator.Generate(name);
            var taken = await db.Clubs.AnyAsync(c => c.Slug == candidate, ct);
            if (!taken) return candidate;
        }

        throw new InvalidOperationException("Не удалось сгенерировать уникальный слаг после 5 попыток.");
    }
}
