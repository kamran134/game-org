using GameOrg.Api.Common;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Events;

/// <summary>
/// CRUD событий и запись участников. Вейтлист/дедлайн записи/отмена —
/// фаза 8.3 (docs/PLAN.md), здесь только базовый join/leave без ограничений
/// по вместимости.
/// </summary>
public sealed class EventService(GameOrgDbContext db)
{
    public async Task<(Event? Result, string? Error)> CreateAsync(Guid userId, CreateEventRequest request, CancellationToken ct)
    {
        var ev = new Event
        {
            PublicId = await GenerateUniquePublicIdAsync(ct),
            Type = request.Type,
            Visibility = request.Visibility,
            SportId = request.SportId,
            ClubId = request.ClubId,
            VenueId = request.VenueId,
            CustomLocation = request.CustomLocation,
            TitleI18n = request.Title?.ToDict(),
            DescriptionI18n = request.Description?.ToDict(),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Timezone = request.Timezone ?? "Asia/Baku",
            MinParticipants = request.MinParticipants,
            MaxParticipants = request.MaxParticipants,
            WaitlistEnabled = request.WaitlistEnabled ?? true,
            SkillLevelMin = request.SkillLevelMin,
            SkillLevelMax = request.SkillLevelMax,
            GenderPolicy = request.GenderPolicy ?? GenderPolicy.Any,
            AgeMin = request.AgeMin,
            AgeMax = request.AgeMax,
            CostSplit = request.CostSplit ?? CostSplit.Free,
            Cost = request.Cost,
            Currency = request.Currency ?? "AZN",
            LockHoursBeforeStart = request.LockHoursBeforeStart,
            CreatedById = userId,
        };

        var error = ValidateInvariants(ev);
        if (error is not null) return (null, error);

        ApplyRegistrationDeadline(ev);

        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
        return (ev, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid eventId, Guid userId, UpdateEventRequest request, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId) return (false, "Редактировать может только создатель.");

        if (request.Visibility is not null) ev.Visibility = request.Visibility.Value;
        if (request.VenueId is not null) ev.VenueId = request.VenueId;
        if (request.CustomLocation is not null) ev.CustomLocation = request.CustomLocation.Length == 0 ? null : request.CustomLocation;
        if (request.Title is not null) ev.TitleI18n = request.Title.ToDict();
        if (request.Description is not null) ev.DescriptionI18n = request.Description.ToDict();
        if (request.StartsAt is not null) ev.StartsAt = request.StartsAt.Value;
        if (request.EndsAt is not null) ev.EndsAt = request.EndsAt.Value;
        if (request.Timezone is not null) ev.Timezone = request.Timezone;
        if (request.MinParticipants is not null) ev.MinParticipants = request.MinParticipants;
        if (request.MaxParticipants is not null) ev.MaxParticipants = request.MaxParticipants;
        if (request.WaitlistEnabled is not null) ev.WaitlistEnabled = request.WaitlistEnabled.Value;
        if (request.SkillLevelMin is not null) ev.SkillLevelMin = request.SkillLevelMin;
        if (request.SkillLevelMax is not null) ev.SkillLevelMax = request.SkillLevelMax;
        if (request.GenderPolicy is not null) ev.GenderPolicy = request.GenderPolicy.Value;
        if (request.AgeMin is not null) ev.AgeMin = request.AgeMin;
        if (request.AgeMax is not null) ev.AgeMax = request.AgeMax;
        if (request.CostSplit is not null) ev.CostSplit = request.CostSplit.Value;
        if (request.Cost is not null) ev.Cost = request.Cost;
        if (request.Currency is not null) ev.Currency = request.Currency;
        if (request.LockHoursBeforeStart is not null) ev.LockHoursBeforeStart = request.LockHoursBeforeStart;

        var error = ValidateInvariants(ev);
        if (error is not null) return (false, error);

        ApplyRegistrationDeadline(ev);

        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<EventDetailDto?> GetByPublicIdAsync(string publicId, string locale, CancellationToken ct)
    {
        var ev = await db.Events
            .Include(e => e.Sport)
            .Include(e => e.Venue)
            .Include(e => e.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.PublicId == publicId, ct);

        return ev is null ? null : MapDetail(ev, locale);
    }

    public async Task<List<EventDto>> GetListAsync(Guid? sportId, Guid? cityId, bool upcoming, string locale, CancellationToken ct)
    {
        // Только Public — тот же охват, что и events_upcoming_public
        // (001_constraints.sql): показ Club/Unlisted событий в общем списке —
        // отдельная, ещё не запланированная задача ("мои события"/клубная лента).
        var query = db.Events.Where(e => e.Visibility == EventVisibility.Public);

        if (sportId is not null) query = query.Where(e => e.SportId == sportId);
        if (cityId is not null) query = query.Where(e => e.Venue != null && e.Venue.CityId == cityId);

        var now = DateTime.UtcNow;
        query = upcoming
            ? query.Where(e => e.StartsAt >= now && e.Status != EventStatus.Cancelled).OrderBy(e => e.StartsAt)
            : query.Where(e => e.StartsAt < now || e.Status == EventStatus.Cancelled).OrderByDescending(e => e.StartsAt);

        var events = await query
            .Include(e => e.Sport)
            .Include(e => e.Venue)
            .Take(50)
            .ToListAsync(ct);

        return events.Select(e => MapListItem(e, locale)).ToList();
    }

    public async Task<(EventParticipantDto? Result, string? Error)> JoinAsync(
        Guid eventId, Guid userId, JoinEventRequest request, string locale, CancellationToken ct)
    {
        if (request.Status is not (ParticipationStatus.Confirmed or ParticipationStatus.Maybe))
            return (null, "Статус может быть только Confirmed или Maybe.");

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (null, "Событие не найдено.");
        if (ev.Status == EventStatus.Cancelled) return (null, "Событие отменено.");

        var already = await db.EventParticipants.AnyAsync(p => p.EventId == eventId && p.UserId == userId, ct);
        if (already) return (null, "Вы уже записаны на это событие.");

        var participant = new EventParticipant { EventId = eventId, UserId = userId, Status = request.Status };
        db.EventParticipants.Add(participant);
        await db.SaveChangesAsync(ct);
        await RecomputeCountsAsync(eventId, ct);

        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        return (new EventParticipantDto(
            participant.Id, userId, Localized.Resolve(user.DisplayNameI18n, locale) ?? "",
            null, participant.Status, null, participant.JoinedAt), null);
    }

    public async Task<bool> LeaveAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        var participant = await db.EventParticipants.FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId, ct);
        if (participant is null) return false;

        db.EventParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        await RecomputeCountsAsync(eventId, ct);
        return true;
    }

    public async Task<(EventParticipantDto? Result, string? Error)> AddGuestAsync(
        Guid eventId, Guid invitedById, AddGuestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return (null, "Имя гостя обязательно.");

        var eventExists = await db.Events.AnyAsync(e => e.Id == eventId, ct);
        if (!eventExists) return (null, "Событие не найдено.");

        var guest = new EventParticipant
        {
            EventId = eventId,
            GuestName = request.GuestName.Trim(),
            InvitedById = invitedById,
            Status = ParticipationStatus.Confirmed,
        };
        db.EventParticipants.Add(guest);
        await db.SaveChangesAsync(ct);
        await RecomputeCountsAsync(eventId, ct);

        return (new EventParticipantDto(guest.Id, null, guest.GuestName, guest.GuestName, guest.Status, null, guest.JoinedAt), null);
    }

    private async Task RecomputeCountsAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return;

        var participants = db.EventParticipants.Where(p => p.EventId == eventId);
        ev.ConfirmedCount = await participants.CountAsync(p => p.Status == ParticipationStatus.Confirmed, ct);
        ev.MaybeCount = await participants.CountAsync(p => p.Status == ParticipationStatus.Maybe, ct);
        ev.WaitlistCount = await participants.CountAsync(p => p.Status == ParticipationStatus.Waitlisted, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Проверки, которые в БД — CHECK-констрейнты (001_constraints.sql) — до похода в БД, с понятным сообщением вместо голого constraint violation.</summary>
    private static string? ValidateInvariants(Event e)
    {
        if (e.EndsAt <= e.StartsAt) return "Время окончания должно быть позже начала.";
        if (e.MaxParticipants is not null && e.MinParticipants is not null && e.MaxParticipants < e.MinParticipants)
            return "Максимум участников не может быть меньше минимума.";
        if (e.VenueId is null && string.IsNullOrWhiteSpace(e.CustomLocation))
            return "Укажите площадку из каталога или произвольное место.";
        if (e.Cost is not null && e.Cost < 0) return "Стоимость не может быть отрицательной.";
        if (e.CostSplit != CostSplit.Free && e.Cost is null) return "Укажите стоимость.";
        if (e.Visibility == EventVisibility.Club && e.ClubId is null) return "Видимость \"Клуб\" требует привязки к клубу.";
        return null;
    }

    /// <summary>Считается один раз при создании/переносе, не на лету при каждом запросе (см. комментарий в домене).</summary>
    private static void ApplyRegistrationDeadline(Event e) =>
        e.RegistrationClosesAt = e.LockHoursBeforeStart is null ? null : e.StartsAt.AddHours(-e.LockHoursBeforeStart.Value);

    private static EventDto MapListItem(Event e, string locale) => new(
        e.Id, e.PublicId, e.Type, e.Status, e.Visibility,
        new SportDto(e.Sport.Id, e.Sport.Slug, e.Sport.NameI18n, e.Sport.Emoji, e.Sport.HasPositions, e.Sport.IsTeamSport),
        e.Venue is null ? null : new EventVenueDto(e.Venue.Id, e.Venue.Slug, Localized.Resolve(e.Venue.NameI18n, locale) ?? ""),
        e.CustomLocation,
        Localized.Resolve(e.TitleI18n, locale),
        e.StartsAt, e.EndsAt, e.Timezone,
        e.MaxParticipants, e.ConfirmedCount, e.Cost, e.Currency);

    private static EventDetailDto MapDetail(Event e, string locale) => new(
        e.Id, e.PublicId, e.Type, e.Status, e.Visibility,
        new SportDto(e.Sport.Id, e.Sport.Slug, e.Sport.NameI18n, e.Sport.Emoji, e.Sport.HasPositions, e.Sport.IsTeamSport),
        e.ClubId,
        e.Venue is null ? null : new EventVenueDto(e.Venue.Id, e.Venue.Slug, Localized.Resolve(e.Venue.NameI18n, locale) ?? ""),
        e.CustomLocation,
        Localized.Resolve(e.TitleI18n, locale),
        Localized.Resolve(e.DescriptionI18n, locale),
        e.StartsAt, e.EndsAt, e.Timezone,
        e.MinParticipants, e.MaxParticipants, e.WaitlistEnabled,
        e.SkillLevelMin, e.SkillLevelMax, e.GenderPolicy, e.AgeMin, e.AgeMax,
        e.CostSplit, e.Cost, e.Currency,
        e.RegistrationOpensAt, e.RegistrationClosesAt, e.LockHoursBeforeStart,
        e.ConfirmedCount, e.MaybeCount, e.WaitlistCount,
        e.CreatedById, e.CancelledAt, e.CancelReason,
        e.Participants.OrderBy(p => p.JoinedAt).Select(p => new EventParticipantDto(
            p.Id, p.UserId,
            p.UserId is not null ? Localized.Resolve(p.User!.DisplayNameI18n, locale) ?? "" : p.GuestName ?? "",
            p.GuestName, p.Status, p.WaitlistOrder, p.JoinedAt)).ToList(),
        LocalizedTextDto.FromNullable(e.TitleI18n),
        LocalizedTextDto.FromNullable(e.DescriptionI18n));

    private async Task<string> GenerateUniquePublicIdAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = EventPublicIdGenerator.Generate();
            var taken = await db.Events.AnyAsync(e => e.PublicId == candidate, ct);
            if (!taken) return candidate;
        }

        throw new InvalidOperationException("Не удалось сгенерировать уникальный PublicId после 5 попыток.");
    }
}
