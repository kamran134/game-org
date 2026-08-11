using GameOrg.Api.Common;
using GameOrg.Api.Features.Events;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Reputation;

/// <summary>
/// Пересчёт Glicko-2 (SportRating) и надёжности (ReliabilityStat) по результату события.
/// Зовётся ровно один раз на событие — из EventService.RecordResultAsync, только пока
/// EventResult.RatingsApplied == false (см. docs/PLAN.md, Шаг 15).
/// </summary>
public sealed class RatingService(GameOrgDbContext db, AchievementService achievementService)
{
    public async Task ApplyResultAsync(Guid eventId, List<StandingEntry>? standings, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return;

        var allUserParticipants = await db.EventParticipants
            .Where(p => p.EventId == eventId && p.UserId != null)
            .ToListAsync(ct);

        // "Играл" — явно Attended, или так и остался Confirmed (организатор не отметил
        // явку отдельно — по умолчанию считаем, что человек был). NoShow/LateCancel/
        // Waitlisted/Declined/Maybe исключены и из рейтинга, и из надёжности.
        var played = allUserParticipants
            .Where(p => p.Status is ParticipationStatus.Attended or ParticipationStatus.Confirmed)
            .ToList();

        var teams = await db.EventTeams.Where(t => t.EventId == eventId).ToListAsync(ct);

        var pairwiseGames = new Dictionary<Guid, List<(Guid Opponent, double Score)>>();
        void AddGame(Guid a, Guid b, double scoreForA)
        {
            if (!pairwiseGames.TryGetValue(a, out var listA)) pairwiseGames[a] = listA = [];
            listA.Add((b, scoreForA));
            if (!pairwiseGames.TryGetValue(b, out var listB)) pairwiseGames[b] = listB = [];
            listB.Add((a, 1 - scoreForA));
        }

        if (teams.Count >= 2 && teams.All(t => t.Score != null))
        {
            var membersByTeam = teams.ToDictionary(t => t.Id, t => played.Where(p => p.TeamId == t.Id).Select(p => p.UserId!.Value).ToList());
            for (var i = 0; i < teams.Count; i++)
            {
                for (var j = i + 1; j < teams.Count; j++)
                {
                    var cmp = teams[i].Score!.Value.CompareTo(teams[j].Score!.Value);
                    var outcomeForI = cmp > 0 ? 1.0 : cmp < 0 ? 0.0 : 0.5;
                    foreach (var userA in membersByTeam[teams[i].Id])
                        foreach (var userB in membersByTeam[teams[j].Id])
                            AddGame(userA, userB, outcomeForI);
                }
            }
        }
        else if (standings is { Count: >= 2 })
        {
            var eligible = played.Select(p => p.UserId!.Value).ToHashSet();
            var ranked = standings.Where(s => eligible.Contains(s.UserId)).ToList();
            for (var i = 0; i < ranked.Count; i++)
            {
                for (var j = i + 1; j < ranked.Count; j++)
                {
                    var outcome = ranked[i].Place < ranked[j].Place ? 1.0 : ranked[i].Place > ranked[j].Place ? 0.0 : 0.5;
                    AddGame(ranked[i].UserId, ranked[j].UserId, outcome);
                }
            }
        }

        if (pairwiseGames.Count > 0)
            await ApplyRatingsAsync(ev.SportId, pairwiseGames, ct);

        foreach (var participant in allUserParticipants)
            await ApplyReliabilityAsync(participant.UserId!.Value, participant.Status, ct);

        if (ev.CreatedById is Guid creatorId)
        {
            var creatorStat = await GetOrCreateReliabilityAsync(creatorId, ct);
            creatorStat.HostedEvents += 1;
            creatorStat.UpdatedAt = DateTime.UtcNow;
        }

        var result = await db.EventResults.FirstAsync(r => r.EventId == eventId, ct);
        result.RatingsApplied = true;

        await db.SaveChangesAsync(ct);

        // После флаша — AchievementService сам делает свежие запросы к SportRating/
        // ReliabilityStat/EventResult, до SaveChangesAsync они бы читались из БД ещё старыми.
        foreach (var participant in allUserParticipants)
            await achievementService.CheckAndAwardAsync(participant.UserId!.Value, eventId, ct);
    }

    private async Task ApplyRatingsAsync(Guid sportId, Dictionary<Guid, List<(Guid Opponent, double Score)>> pairwiseGames, CancellationToken ct)
    {
        var userIds = pairwiseGames.Keys.ToList();
        var existing = await db.SportRatings.Where(r => r.SportId == sportId && userIds.Contains(r.UserId)).ToDictionaryAsync(r => r.UserId, ct);

        // Снимок ДО пересчёта — иначе игроки одного события считались бы друг против
        // друга уже с обновлённым рейтингом в зависимости от порядка обхода словаря.
        Glicko2.Rating SnapshotOf(Guid userId) => existing.TryGetValue(userId, out var r)
            ? new Glicko2.Rating(r.Rating, r.Deviation, r.Volatility)
            : new Glicko2.Rating(1500, 350, 0.06);
        var snapshot = userIds.ToDictionary(id => id, SnapshotOf);

        foreach (var userId in userIds)
        {
            var games = pairwiseGames[userId].Select(g => new Glicko2.GameResult(snapshot[g.Opponent], g.Score)).ToList();
            var updated = Glicko2.Calculate(snapshot[userId], games);

            if (!existing.TryGetValue(userId, out var rating))
            {
                rating = new SportRating { UserId = userId, SportId = sportId };
                db.SportRatings.Add(rating);
            }

            rating.Rating = updated.Value;
            rating.Deviation = updated.Deviation;
            rating.Volatility = updated.Volatility;
            rating.GamesPlayed += 1;
            rating.Wins += games.Count(g => g.Score == 1.0);
            rating.Draws += games.Count(g => g.Score == 0.5);
            rating.Losses += games.Count(g => g.Score == 0.0);
            rating.PeakRating = Math.Max(rating.PeakRating, updated.Value);
            rating.LastPlayedAt = DateTime.UtcNow;
            rating.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task ApplyReliabilityAsync(Guid userId, ParticipationStatus status, CancellationToken ct)
    {
        var stat = await GetOrCreateReliabilityAsync(userId, ct);

        stat.Signups += 1;
        if (status is ParticipationStatus.Attended or ParticipationStatus.Confirmed) stat.Attended += 1;
        else if (status == ParticipationStatus.NoShow) stat.NoShows += 1;
        else if (status == ParticipationStatus.LateCancel) stat.LateCancels += 1;

        stat.Score = ReliabilityCalculator.ComputeScore(stat.Attended, stat.NoShows, stat.LateCancels);

        var attendedDates = await db.EventParticipants
            .Where(p => p.UserId == userId && (p.Status == ParticipationStatus.Attended || p.Status == ParticipationStatus.Confirmed))
            .Join(db.Events, p => p.EventId, e => e.Id, (p, e) => e.StartsAt)
            .ToListAsync(ct);
        var (current, longest) = ReliabilityCalculator.ComputeStreaks(attendedDates, DateTime.UtcNow);
        stat.CurrentStreak = current;
        stat.LongestStreak = longest;

        stat.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<ReliabilityStat> GetOrCreateReliabilityAsync(Guid userId, CancellationToken ct)
    {
        var stat = await db.ReliabilityStats.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (stat is not null) return stat;

        stat = new ReliabilityStat { UserId = userId };
        db.ReliabilityStats.Add(stat);
        return stat;
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(string sportSlug, string locale, int take, CancellationToken ct)
    {
        var sport = await db.Sports.FirstOrDefaultAsync(s => s.Slug == sportSlug, ct);
        if (sport is null) return [];

        var top = await db.SportRatings
            .Where(r => r.SportId == sport.Id)
            .OrderByDescending(r => r.Rating)
            .Take(Math.Clamp(take, 1, 100))
            .Select(r => new { r.UserId, r.User.Handle, r.User.DisplayNameI18n, r.Rating, r.GamesPlayed, r.Wins, r.Draws, r.Losses })
            .ToListAsync(ct);

        return top.Select(r => new LeaderboardEntryDto(
            r.UserId, r.Handle, Localized.Resolve(r.DisplayNameI18n, locale) ?? r.Handle,
            r.Rating, r.GamesPlayed, r.Wins, r.Draws, r.Losses)).ToList();
    }
}
