using GameOrg.Api.Common;
using GameOrg.Api.Features.Clubs;
using GameOrg.Api.Features.Moderation;
using GameOrg.Api.Features.Payments;
using GameOrg.Api.Features.Reputation;
using GameOrg.Api.Features.Social;
using GameOrg.Api.Features.Sports;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Events;

/// <summary>CRUD событий, запись участников (с вейтлистом), отмена. Напоминания за 24ч/2ч — EventReminderJob.</summary>
public sealed class EventService(
    GameOrgDbContext db,
    NotificationSender notificationSender,
    AuditLogService auditLog,
    ClubService clubService,
    ActivityService activityService,
    RatingService ratingService,
    PaymentService paymentService)
{
    public async Task<(Event? Result, string? Error)> CreateAsync(Guid userId, CreateEventRequest request, CancellationToken ct)
    {
        if (request.ClubId is not null && !await clubService.IsActiveMemberAsync(request.ClubId.Value, userId, ct))
            return (null, "Привязать событие можно только к клубу, где вы участник.");

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
            // Public по умолчанию требует одобрения организатором; Club/Unlisted —
            // аудитория уже закрытая, форсим false независимо от запроса (Шаг 19).
            RequiresApproval = request.Visibility == EventVisibility.Public && (request.RequiresApproval ?? true),
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

        if (ev.Visibility == EventVisibility.Public)
            await activityService.EmitAsync(userId, ActivityVerb.CreatedEvent, ev.Id, null, null, null, ct);

        return (ev, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(Guid eventId, Guid userId, UpdateEventRequest request, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId) return (false, "Редактировать может только создатель.");

        var startsAtBefore = ev.StartsAt;
        var endsAtBefore = ev.EndsAt;
        var venueIdBefore = ev.VenueId;
        var customLocationBefore = ev.CustomLocation;

        if (request.Visibility is not null) ev.Visibility = request.Visibility.Value;
        if (request.RequiresApproval is not null) ev.RequiresApproval = request.RequiresApproval.Value;
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

        // Club/Unlisted — закрытая аудитория, approval там бессмысленен (Шаг 19),
        // независимо от того, что стоит в базе или пришло в этом же запросе.
        if (ev.Visibility != EventVisibility.Public) ev.RequiresApproval = false;

        var error = ValidateInvariants(ev);
        if (error is not null) return (false, error);

        ApplyRegistrationDeadline(ev);

        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var scheduleChanged = ev.StartsAt != startsAtBefore || ev.EndsAt != endsAtBefore
            || ev.VenueId != venueIdBefore || ev.CustomLocation != customLocationBefore;
        if (scheduleChanged)
        {
            var participantUserIds = await db.EventParticipants
                .Where(p => p.EventId == eventId && p.UserId != null
                    && (p.Status == ParticipationStatus.Confirmed || p.Status == ParticipationStatus.Maybe))
                .Select(p => p.UserId!.Value)
                .ToListAsync(ct);

            foreach (var participantUserId in participantUserIds)
            {
                await notificationSender.SendAsync(
                    participantUserId,
                    NotificationType.EventUpdated,
                    $"EVENT_UPDATED:{eventId}:{participantUserId}:{ev.UpdatedAt.Ticks}",
                    "Детали события изменились — время или место.",
                    new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                    ct);
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Visibility.Club — видно только активным участникам этого клуба (тот же
    /// viewerId-aware приём, что VenueService.GetBySlugAsync для Draft): 404,
    /// не 403, чужому Club-событию как будто не существует.
    /// </summary>
    public async Task<EventDetailDto?> GetByPublicIdAsync(string publicId, string locale, Guid? viewerId, CancellationToken ct)
    {
        var ev = await db.Events
            .Include(e => e.Sport)
            .Include(e => e.Venue)
            .Include(e => e.Participants).ThenInclude(p => p.User)
            .Include(e => e.Teams)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.PublicId == publicId, ct);

        if (ev is null) return null;

        if (ev.Visibility == EventVisibility.Club)
        {
            var isMember = ev.ClubId is not null && viewerId is not null
                && await clubService.IsActiveMemberAsync(ev.ClubId.Value, viewerId.Value, ct);
            if (!isMember) return null;
        }

        var mvpTally = await GetMvpTallyAsync(ev.Id, locale, ct);
        var myVote = viewerId is null
            ? null
            : await db.MvpVotes.Where(v => v.EventId == ev.Id && v.VoterId == viewerId).Select(v => (Guid?)v.TargetUserId).FirstOrDefaultAsync(ct);

        string? mvpDisplayName = null;
        if (ev.Result?.MvpUserId is Guid mvpId)
        {
            var mvpUser = ev.Participants.FirstOrDefault(p => p.UserId == mvpId)?.User;
            mvpDisplayName = mvpUser is not null ? Localized.Resolve(mvpUser.DisplayNameI18n, locale) : null;
        }

        var myPayment = viewerId is null ? null : await paymentService.GetMyPaymentForEventAsync(ev.Id, viewerId.Value, ct);

        return MapDetail(ev, locale, mvpDisplayName, mvpTally, myVote, myPayment);
    }

    /// <summary>
    /// Только участники с UserId (не гости), голосовавшие — от нуля и выше. Используется и для
    /// EventDetailDto.MvpTally (все видят), и для авторезолва MvpUserId в RecordResultAsync.
    /// </summary>
    private async Task<List<MvpTallyEntryDto>> GetMvpTallyAsync(Guid eventId, string locale, CancellationToken ct)
    {
        var raw = await db.MvpVotes
            .Where(v => v.EventId == eventId)
            .GroupBy(v => v.TargetUserId)
            .Select(g => new { UserId = g.Key, Votes = g.Count() })
            .OrderByDescending(g => g.Votes)
            .ToListAsync(ct);

        if (raw.Count == 0) return [];

        var userIds = raw.Select(r => r.UserId).ToList();
        var users = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Handle, u.DisplayNameI18n }).ToListAsync(ct);
        var byId = users.ToDictionary(u => u.Id);

        return raw.Select(r => new MvpTallyEntryDto(
            r.UserId,
            byId.TryGetValue(r.UserId, out var u) ? Localized.Resolve(u.DisplayNameI18n, locale) ?? u.Handle : "",
            r.Votes)).ToList();
    }

    /// <summary>Создатель или модератор, только после EndsAt — иначе непонятно, что вообще завершать.</summary>
    public async Task<(bool Ok, string? Error)> CompleteAsync(Guid eventId, Guid userId, bool isModerator, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId && !isModerator) return (false, "Завершить может только создатель или модератор.");
        if (ev.Status is not (EventStatus.Scheduled or EventStatus.Confirmed))
            return (false, "Завершить можно только запланированное или подтверждённое событие.");
        if (DateTime.UtcNow < ev.EndsAt) return (false, "Событие ещё не закончилось.");

        ev.Status = EventStatus.Completed;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (ev.Visibility == EventVisibility.Public)
            await activityService.EmitAsync(userId, ActivityVerb.CompletedEvent, ev.Id, null, null, null, ct);

        var participantUserIds = await db.EventParticipants
            .Where(p => p.EventId == eventId && p.UserId != null && (p.Status == ParticipationStatus.Confirmed || p.Status == ParticipationStatus.Attended))
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);

        foreach (var participantUserId in participantUserIds)
        {
            await notificationSender.SendAsync(
                participantUserId,
                NotificationType.MvpVoteOpen,
                $"MVP_VOTE_OPEN:{eventId}:{participantUserId}",
                "Событие завершено — проголосуйте за MVP.",
                new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                ct);
        }

        return (true, null);
    }

    /// <summary>
    /// Полная замена состава — как club.Sports.Clear()+Add в ClubService. Обнуляет TeamId у всех
    /// участников события ExecuteUpdateAsync'ом до удаления старых команд — иначе осиротевшие
    /// ссылки на уже удалённые EventTeam.
    /// </summary>
    public async Task<(bool Ok, string? Error)> SetTeamsAsync(Guid eventId, Guid userId, bool isModerator, SetTeamsRequest request, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId && !isModerator) return (false, "Управлять командами может только создатель или модератор.");

        var participantIds = request.Teams.SelectMany(t => t.ParticipantIds).ToList();
        if (participantIds.Count != participantIds.Distinct().Count())
            return (false, "Участник не может быть в двух командах одновременно.");

        if (participantIds.Count > 0)
        {
            var validCount = await db.EventParticipants.CountAsync(p => p.EventId == eventId && participantIds.Contains(p.Id), ct);
            if (validCount != participantIds.Count) return (false, "Один или несколько участников не относятся к этому событию.");
        }

        var existingTeams = await db.EventTeams.Where(t => t.EventId == eventId).ToListAsync(ct);
        db.EventTeams.RemoveRange(existingTeams);

        await db.EventParticipants.Where(p => p.EventId == eventId && p.TeamId != null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TeamId, (Guid?)null), ct);

        var teams = request.Teams
            .Select(t => new EventTeam { EventId = eventId, Name = t.Name, ColorHex = t.ColorHex, SortOrder = t.SortOrder })
            .ToList();
        db.EventTeams.AddRange(teams);
        await db.SaveChangesAsync(ct);

        for (var i = 0; i < request.Teams.Count; i++)
        {
            var memberIds = request.Teams[i].ParticipantIds;
            if (memberIds.Count == 0) continue;

            var teamId = teams[i].Id;
            await db.EventParticipants.Where(p => memberIds.Contains(p.Id)).ExecuteUpdateAsync(s => s.SetProperty(p => p.TeamId, teamId), ct);
        }

        return (true, null);
    }

    /// <summary>Голосуют только участники события (не гости) друг за друга, только после Completed. Повторный голос меняет цель.</summary>
    public async Task<(bool Ok, string? Error)> VoteMvpAsync(Guid eventId, Guid voterId, Guid targetUserId, CancellationToken ct)
    {
        if (voterId == targetUserId) return (false, "Нельзя голосовать за самого себя.");

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.Status != EventStatus.Completed) return (false, "Голосование открывается после завершения события.");

        var voterIsParticipant = await db.EventParticipants.AnyAsync(p => p.EventId == eventId && p.UserId == voterId, ct);
        if (!voterIsParticipant) return (false, "Голосовать может только участник события.");

        var targetIsParticipant = await db.EventParticipants.AnyAsync(p => p.EventId == eventId && p.UserId == targetUserId, ct);
        if (!targetIsParticipant) return (false, "Голосовать можно только за участника события.");

        var existing = await db.MvpVotes.FirstOrDefaultAsync(v => v.EventId == eventId && v.VoterId == voterId, ct);
        if (existing is null)
            db.MvpVotes.Add(new MvpVote { EventId = eventId, VoterId = voterId, TargetUserId = targetUserId });
        else
            existing.TargetUserId = targetUserId;

        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    /// <summary>
    /// Создатель/модератор, только для Completed. MvpUserId в запросе — явное переопределение,
    /// не передан — берётся победитель голосования на момент записи (GetMvpTallyAsync, первая
    /// строка — уже отсортирована по убыванию голосов). EventResult.EventId — PK, поэтому повторный
    /// вызов редактирует уже записанный результат, а не 409-ит.
    /// </summary>
    public async Task<(bool Ok, string? Error)> RecordResultAsync(Guid eventId, Guid userId, bool isModerator, RecordResultRequest request, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId && !isModerator) return (false, "Записать результат может только создатель или модератор.");
        if (ev.Status != EventStatus.Completed) return (false, "Результат можно записать только для завершённого события.");

        if (request.TeamScores is { Count: > 0 })
        {
            var teamIds = request.TeamScores.Select(s => s.TeamId).ToList();
            var validTeamCount = await db.EventTeams.CountAsync(t => t.EventId == eventId && teamIds.Contains(t.Id), ct);
            if (validTeamCount != teamIds.Distinct().Count()) return (false, "Одна или несколько команд не относятся к этому событию.");

            foreach (var scoreEntry in request.TeamScores)
                await db.EventTeams.Where(t => t.Id == scoreEntry.TeamId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Score, scoreEntry.Score), ct);
        }

        if (request.Attendance is { Count: > 0 })
        {
            if (request.Attendance.Any(a => a.Status is not (ParticipationStatus.Attended or ParticipationStatus.NoShow or ParticipationStatus.LateCancel)))
                return (false, "Отметка посещаемости — только Attended, NoShow или LateCancel.");

            var participantIds = request.Attendance.Select(a => a.ParticipantId).ToList();
            var validParticipantCount = await db.EventParticipants.CountAsync(p => p.EventId == eventId && participantIds.Contains(p.Id), ct);
            if (validParticipantCount != participantIds.Distinct().Count()) return (false, "Один или несколько участников не относятся к этому событию.");

            var now = DateTime.UtcNow;
            foreach (var entry in request.Attendance)
            {
                await db.EventParticipants.Where(p => p.Id == entry.ParticipantId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, entry.Status).SetProperty(p => p.StatusChangedAt, now), ct);
            }
        }

        var mvpUserId = request.MvpUserId;
        if (mvpUserId is null)
        {
            var tally = await GetMvpTallyAsync(eventId, RequestLocale.Default, ct);
            mvpUserId = tally.Count > 0 ? tally[0].UserId : null;
        }

        var result = await db.EventResults.FirstOrDefaultAsync(r => r.EventId == eventId, ct);
        if (result is null)
        {
            result = new EventResult { EventId = eventId };
            db.EventResults.Add(result);
        }
        var alreadyRated = result.RatingsApplied;

        result.Summary = request.Summary;
        result.Standings = request.Standings?.Select(s =>
        {
            var entry = new Dictionary<string, object> { ["userId"] = s.UserId.ToString(), ["place"] = s.Place };
            if (s.Score is not null) entry["score"] = s.Score.Value;
            return entry;
        }).ToList();
        result.MvpUserId = mvpUserId;
        result.RecordedById = userId;
        result.RecordedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Только один раз на событие — правка результата задним числом рейтинг не трогает
        // (EventResult.RatingsApplied — идемпотентность, см. docs/PLAN.md, Шаг 15).
        if (!alreadyRated)
            await ratingService.ApplyResultAsync(eventId, request.Standings, ct);

        var participantUserIds = await db.EventParticipants
            .Where(p => p.EventId == eventId && p.UserId != null)
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);

        foreach (var participantUserId in participantUserIds)
        {
            await notificationSender.SendAsync(
                participantUserId,
                NotificationType.ResultPosted,
                $"RESULT_POSTED:{eventId}:{participantUserId}:{result.RecordedAt.Ticks}",
                "Результат события опубликован.",
                new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                ct);
        }

        return (true, null);
    }

    public async Task<List<EventDto>> GetListAsync(Guid? sportId, Guid? cityId, bool upcoming, string locale, Guid? viewerId, CancellationToken ct)
    {
        // Public — всем; Unlisted — отдельная задача ("по ссылке", ещё не
        // запланирована); Club — только активным участникам этого клуба.
        var viewerClubIds = viewerId is null
            ? []
            : await db.ClubMembers
                .Where(m => m.UserId == viewerId && m.Status == MembershipStatus.Active)
                .Select(m => m.ClubId)
                .ToListAsync(ct);

        var query = db.Events.Where(e =>
            e.Visibility == EventVisibility.Public
            || (e.Visibility == EventVisibility.Club && e.ClubId != null && viewerClubIds.Contains(e.ClubId.Value)));

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

        var openError = CheckRegistrationOpen(ev);
        if (openError is not null) return (null, openError);

        var already = await db.EventParticipants.AnyAsync(p => p.EventId == eventId && p.UserId == userId, ct);
        if (already) return (null, "Вы уже записаны на это событие.");

        // Создатель себя не спрашивает; PendingApproval минует резолвинг вейтлиста
        // целиком — место/вейтлист решаются в момент ApproveJoinAsync, не сейчас.
        var needsApproval = ev.RequiresApproval && ev.CreatedById != userId;

        var (status, waitlistOrder, capacityError) = needsApproval
            ? (ParticipationStatus.PendingApproval, (int?)null, (string?)null)
            : await ResolveJoinStatusAsync(ev, request.Status, ct);
        if (capacityError is not null) return (null, capacityError);

        var participant = new EventParticipant { EventId = eventId, UserId = userId, Status = status, WaitlistOrder = waitlistOrder };
        db.EventParticipants.Add(participant);
        await db.SaveChangesAsync(ct);

        if (status != ParticipationStatus.PendingApproval)
            await RecomputeCountsAsync(eventId, ct);

        if (status == ParticipationStatus.Confirmed)
        {
            await paymentService.EnsurePaymentAsync(eventId, participant.Id, ct);
            await paymentService.RecomputeTotalSplitAsync(eventId, ct);
        }

        if (status == ParticipationStatus.Confirmed && ev.Visibility == EventVisibility.Public)
            await activityService.EmitAsync(userId, ActivityVerb.JoinedEvent, eventId, null, null, null, ct);

        if (ev.CreatedById is Guid creatorId && creatorId != userId)
        {
            await notificationSender.SendAsync(
                creatorId,
                status == ParticipationStatus.PendingApproval ? NotificationType.EventJoinRequest : NotificationType.ParticipantJoined,
                status == ParticipationStatus.PendingApproval
                    ? $"EVENT_JOIN_REQUEST:{eventId}:{participant.Id}"
                    : $"PARTICIPANT_JOINED:{eventId}:{participant.Id}",
                status == ParticipationStatus.PendingApproval ? "Новая заявка на ваше событие." : "Новая запись на ваше событие.",
                new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                ct);
        }

        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
        return (new EventParticipantDto(
            participant.Id, userId, Localized.Resolve(user.DisplayNameI18n, locale) ?? "",
            null, participant.Status, participant.WaitlistOrder, participant.JoinedAt), null);
    }

    /// <summary>Заявки, ждущие решения — организатор, владелец/админ клуба события или модератор.</summary>
    public async Task<(List<EventJoinRequestDto>? Result, string? Error)> GetJoinRequestsAsync(
        Guid eventId, Guid viewerId, bool isModerator, string locale, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (null, "Событие не найдено.");
        if (!await CanManageJoinRequestsAsync(ev, viewerId, isModerator, ct)) return (null, "Заявки видит только организатор, админ клуба или модератор.");

        var requests = await db.EventParticipants
            .Where(p => p.EventId == eventId && p.Status == ParticipationStatus.PendingApproval)
            .Include(p => p.User)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(ct);

        return (requests.Select(p => new EventJoinRequestDto(
            p.Id, p.UserId!.Value, Localized.Resolve(p.User!.DisplayNameI18n, locale) ?? "", p.JoinedAt)).ToList(), null);
    }

    /// <summary>Одобрение — обычный конвейер вейтлиста (полное событие уводит в очередь, не переполняет).</summary>
    public async Task<(bool Ok, string? Error)> ApproveJoinAsync(Guid eventId, Guid participantId, Guid actorId, bool isModerator, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (!await CanManageJoinRequestsAsync(ev, actorId, isModerator, ct)) return (false, "Одобрять заявки может только организатор, админ клуба или модератор.");

        var participant = await db.EventParticipants.FirstOrDefaultAsync(
            p => p.Id == participantId && p.EventId == eventId && p.Status == ParticipationStatus.PendingApproval, ct);
        if (participant is null) return (false, "Заявка не найдена.");

        var (status, waitlistOrder, capacityError) = await ResolveJoinStatusAsync(ev, ParticipationStatus.Confirmed, ct);
        if (capacityError is not null) return (false, capacityError);

        participant.Status = status;
        participant.WaitlistOrder = waitlistOrder;
        participant.StatusChangedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecomputeCountsAsync(eventId, ct);

        if (status == ParticipationStatus.Confirmed)
        {
            await paymentService.EnsurePaymentAsync(eventId, participant.Id, ct);
            await paymentService.RecomputeTotalSplitAsync(eventId, ct);
            if (ev.Visibility == EventVisibility.Public)
                await activityService.EmitAsync(participant.UserId!.Value, ActivityVerb.JoinedEvent, eventId, null, null, null, ct);
        }

        await notificationSender.SendAsync(
            participant.UserId!.Value, NotificationType.EventJoinApproved,
            $"EVENT_JOIN_APPROVED:{eventId}:{participant.Id}",
            "Ваша заявка на событие одобрена.",
            new Dictionary<string, object> { ["eventId"] = eventId.ToString() }, ct);

        return (true, null);
    }

    /// <summary>Отклонение удаляет заявку — не тумбстоун, человек может подать заново.</summary>
    public async Task<(bool Ok, string? Error)> RejectJoinAsync(Guid eventId, Guid participantId, Guid actorId, bool isModerator, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (!await CanManageJoinRequestsAsync(ev, actorId, isModerator, ct)) return (false, "Отклонять заявки может только организатор, админ клуба или модератор.");

        var participant = await db.EventParticipants.FirstOrDefaultAsync(
            p => p.Id == participantId && p.EventId == eventId && p.Status == ParticipationStatus.PendingApproval, ct);
        if (participant is null) return (false, "Заявка не найдена.");

        var requesterId = participant.UserId!.Value;
        db.EventParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);

        await notificationSender.SendAsync(
            requesterId, NotificationType.EventJoinRejected,
            $"EVENT_JOIN_REJECTED:{eventId}:{participantId}",
            "Ваша заявка на событие отклонена.",
            new Dictionary<string, object> { ["eventId"] = eventId.ToString() }, ct);

        return (true, null);
    }

    private async Task<bool> CanManageJoinRequestsAsync(Event ev, Guid userId, bool isModerator, CancellationToken ct)
    {
        if (isModerator || ev.CreatedById == userId) return true;
        return ev.ClubId is Guid clubId && await clubService.IsOwnerOrAdminAsync(clubId, userId, ct);
    }

    public async Task<bool> LeaveAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        var participant = await db.EventParticipants.FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId, ct);
        if (participant is null) return false;
        var participantId = participant.Id;

        db.EventParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        await paymentService.CancelPendingForParticipantAsync(participantId, ct);
        await RecomputeCountsAsync(eventId, ct);
        await paymentService.RecomputeTotalSplitAsync(eventId, ct);
        // Освободившийся Confirmed-слот (если он был) может освободить место
        // под первого в очереди — метод сам разбирается, есть ли вообще
        // свободные места и очередь; безопасно звать всегда, а не только
        // когда точно знаем, что участник был Confirmed.
        await PromoteFromWaitlistAsync(eventId, ct);

        var createdById = await db.Events.Where(e => e.Id == eventId).Select(e => e.CreatedById).FirstOrDefaultAsync(ct);
        if (createdById is Guid creatorId && creatorId != userId)
        {
            await notificationSender.SendAsync(
                creatorId,
                NotificationType.ParticipantLeft,
                $"PARTICIPANT_LEFT:{eventId}:{participantId}",
                "Кто-то отменил запись на ваше событие.",
                new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                ct);
        }
        return true;
    }

    public async Task<(EventParticipantDto? Result, string? Error)> AddGuestAsync(
        Guid eventId, Guid invitedById, AddGuestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return (null, "Имя гостя обязательно.");

        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (null, "Событие не найдено.");

        var openError = CheckRegistrationOpen(ev);
        if (openError is not null) return (null, openError);

        // Гость всегда "просится" как Confirmed — тот же вейтлист-конвейер,
        // что и self-join, иначе приглашение гостя обходило бы MaxParticipants.
        var (status, waitlistOrder, capacityError) = await ResolveJoinStatusAsync(ev, ParticipationStatus.Confirmed, ct);
        if (capacityError is not null) return (null, capacityError);

        var guest = new EventParticipant
        {
            EventId = eventId,
            GuestName = request.GuestName.Trim(),
            InvitedById = invitedById,
            Status = status,
            WaitlistOrder = waitlistOrder,
        };
        db.EventParticipants.Add(guest);
        await db.SaveChangesAsync(ct);
        await RecomputeCountsAsync(eventId, ct);
        // У гостя нет UserId — платить ему не выставляем (EnsurePaymentAsync это и так
        // отсекает), но он всё равно занимает место в ConfirmedCount — при CostSplit.Total
        // это меняет сумму на человека для остальных.
        if (status == ParticipationStatus.Confirmed)
            await paymentService.RecomputeTotalSplitAsync(eventId, ct);

        return (new EventParticipantDto(guest.Id, null, guest.GuestName, guest.GuestName, guest.Status, guest.WaitlistOrder, guest.JoinedAt), null);
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(Guid eventId, Guid userId, string? reason, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId) return (false, "Отменить может только создатель.");
        if (ev.Status == EventStatus.Cancelled) return (false, "Событие уже отменено.");

        ev.Status = EventStatus.Cancelled;
        ev.CancelledAt = DateTime.UtcNow;
        ev.CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        ev.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await paymentService.CancelAllPendingForEventAsync(eventId, ct);

        // Немедленно (не по расписанию, как напоминания) — SendAsync сам
        // идемпотентен по DedupeKey, повторный вызов CancelAsync (если бы
        // он был возможен — выше уже отсечён статусом) ничего не продублирует.
        var participantUserIds = await db.EventParticipants
            .Where(p => p.EventId == eventId && p.UserId != null && p.Status != ParticipationStatus.Declined)
            .Select(p => p.UserId!.Value)
            .ToListAsync(ct);

        foreach (var participantUserId in participantUserIds)
        {
            await notificationSender.SendAsync(
                participantUserId,
                NotificationType.EventCancelled,
                $"EVENT_CANCELLED:{eventId}:{participantUserId}",
                "Событие отменено.",
                new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                ct);
        }

        return (true, null);
    }

    /// <summary>Автор или модератор. Soft-delete — DeletedAt, глобальный HasQueryFilter уже прячет такие строки из всех выдач.
    /// Отличается от CancelAsync: cancel уведомляет участников («не состоится»), delete молча убирает мусор.</summary>
    public async Task<(bool Ok, string? Error)> DeleteAsync(Guid eventId, Guid userId, bool isModerator, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (false, "Событие не найдено.");
        if (ev.CreatedById != userId && !isModerator) return (false, "Удалить может только автор или модератор.");

        ev.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditLog.LogAsync(userId, "event.delete", nameof(Event), ev.Id, null, null, ct);
        return (true, null);
    }

    private static string? CheckRegistrationOpen(Event ev)
    {
        if (ev.Status == EventStatus.Cancelled) return "Событие отменено.";
        if (ev.RegistrationClosesAt is not null && DateTime.UtcNow > ev.RegistrationClosesAt) return "Запись на событие закрыта.";
        return null;
    }

    /// <summary>
    /// Maybe никогда не занимает место — вейтлист только про Confirmed.
    /// Свободных мест нет и WaitlistEnabled — уходит в Waitlisted с
    /// следующим WaitlistOrder (participants_waitlist_uq в БД не даст
    /// коллизии, но мы и так считаем максимум явно, а не полагаемся на retry).
    /// </summary>
    private async Task<(ParticipationStatus Status, int? WaitlistOrder, string? Error)> ResolveJoinStatusAsync(
        Event ev, ParticipationStatus requested, CancellationToken ct)
    {
        if (requested != ParticipationStatus.Confirmed) return (requested, null, null);
        if (ev.MaxParticipants is null || ev.ConfirmedCount < ev.MaxParticipants) return (ParticipationStatus.Confirmed, null, null);
        if (!ev.WaitlistEnabled) return (ParticipationStatus.Confirmed, null, "Свободных мест нет.");

        var lastOrder = await db.EventParticipants
            .Where(p => p.EventId == ev.Id && p.WaitlistOrder != null)
            .Select(p => (int?)p.WaitlistOrder)
            .MaxAsync(ct) ?? 0;
        return (ParticipationStatus.Waitlisted, lastOrder + 1, null);
    }

    /// <summary>По образцу RecomputeRatingAsync в VenueService — отдельный шаг после изменения состава, не инкрементальный счётчик.</summary>
    private async Task PromoteFromWaitlistAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null || ev.MaxParticipants is null || ev.ConfirmedCount >= ev.MaxParticipants) return;

        var next = await db.EventParticipants
            .Where(p => p.EventId == eventId && p.Status == ParticipationStatus.Waitlisted)
            .OrderBy(p => p.WaitlistOrder)
            .FirstOrDefaultAsync(ct);
        if (next is null) return;

        next.Status = ParticipationStatus.Confirmed;
        next.WaitlistOrder = null;
        next.StatusChangedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecomputeCountsAsync(eventId, ct);
        await paymentService.EnsurePaymentAsync(eventId, next.Id, ct);
        await paymentService.RecomputeTotalSplitAsync(eventId, ct);

        if (next.UserId is not null)
        {
            await notificationSender.SendAsync(
                next.UserId.Value,
                NotificationType.WaitlistPromoted,
                $"WAITLIST_PROMOTED:{eventId}:{next.Id}",
                "Освободилось место — вы переведены из листа ожидания в участники.",
                new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                ct);
        }
    }

    private async Task RecomputeCountsAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return;

        var participants = db.EventParticipants.Where(p => p.EventId == eventId);
        ev.ConfirmedCount = await participants.CountAsync(p => p.Status == ParticipationStatus.Confirmed, ct);
        ev.MaybeCount = await participants.CountAsync(p => p.Status == ParticipationStatus.Maybe, ct);
        ev.WaitlistCount = await participants.CountAsync(p => p.Status == ParticipationStatus.Waitlisted, ct);

        // Только вперёд, без отката — если потом кто-то вышел и счётчик упал
        // ниже MinParticipants, статус остаётся Confirmed (решение, Шаг 11):
        // не дёргать статус туда-сюда и не спамить "снова не подтверждено".
        var justConfirmed = ev.Status == EventStatus.Scheduled && ev.MinParticipants != null && ev.ConfirmedCount >= ev.MinParticipants;
        if (justConfirmed) ev.Status = EventStatus.Confirmed;

        await db.SaveChangesAsync(ct);

        if (justConfirmed)
        {
            var confirmedUserIds = await participants
                .Where(p => p.Status == ParticipationStatus.Confirmed && p.UserId != null)
                .Select(p => p.UserId!.Value)
                .ToListAsync(ct);

            foreach (var confirmedUserId in confirmedUserIds)
            {
                await notificationSender.SendAsync(
                    confirmedUserId,
                    NotificationType.EventConfirmed,
                    $"EVENT_CONFIRMED:{eventId}:{confirmedUserId}",
                    "Набралось достаточно участников — событие подтверждено.",
                    new Dictionary<string, object> { ["eventId"] = eventId.ToString() },
                    ct);
            }
        }
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

    private static EventDetailDto MapDetail(
        Event e, string locale, string? mvpDisplayName, List<MvpTallyEntryDto> mvpTally, Guid? myVote, PaymentSummaryDto? myPayment) => new(
        e.Id, e.PublicId, e.Type, e.Status, e.Visibility,
        new SportDto(e.Sport.Id, e.Sport.Slug, e.Sport.NameI18n, e.Sport.Emoji, e.Sport.HasPositions, e.Sport.IsTeamSport),
        e.ClubId,
        e.Venue is null ? null : new EventVenueDto(e.Venue.Id, e.Venue.Slug, Localized.Resolve(e.Venue.NameI18n, locale) ?? ""),
        e.CustomLocation,
        Localized.Resolve(e.TitleI18n, locale),
        Localized.Resolve(e.DescriptionI18n, locale),
        e.StartsAt, e.EndsAt, e.Timezone,
        e.MinParticipants, e.MaxParticipants, e.WaitlistEnabled, e.RequiresApproval,
        e.SkillLevelMin, e.SkillLevelMax, e.GenderPolicy, e.AgeMin, e.AgeMax,
        e.CostSplit, e.Cost, e.Currency,
        e.RegistrationOpensAt, e.RegistrationClosesAt, e.LockHoursBeforeStart,
        e.ConfirmedCount, e.MaybeCount, e.WaitlistCount,
        e.CreatedById, e.CancelledAt, e.CancelReason,
        // PendingApproval не в общем списке — кто подал заявку, видит только тот,
        // кто её разбирает (GetJoinRequestsAsync); не светим ждущих одобрения
        // всем подряд посетителям страницы (Шаг 19). Сам заявитель узнаёт статус
        // из ответа JoinAsync сразу после отправки — до решения организатора
        // страница при перезагрузке этого не покажет, известное упрощение.
        e.Participants.Where(p => p.Status != ParticipationStatus.PendingApproval).OrderBy(p => p.JoinedAt).Select(p => new EventParticipantDto(
            p.Id, p.UserId,
            p.UserId is not null ? Localized.Resolve(p.User!.DisplayNameI18n, locale) ?? "" : p.GuestName ?? "",
            p.GuestName, p.Status, p.WaitlistOrder, p.JoinedAt)).ToList(),
        e.Teams.OrderBy(t => t.SortOrder).Select(t => new EventTeamDto(
            t.Id, t.Name, t.ColorHex, t.Score, t.SortOrder,
            e.Participants.Where(p => p.TeamId == t.Id).Select(p => p.Id).ToList())).ToList(),
        e.Result is null ? null : new EventResultDto(e.Result.Summary, e.Result.Standings, e.Result.MvpUserId, mvpDisplayName, e.Result.RecordedById, e.Result.RecordedAt),
        mvpTally,
        myVote,
        myPayment,
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
