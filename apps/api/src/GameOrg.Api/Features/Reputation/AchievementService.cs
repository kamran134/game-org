using GameOrg.Api.Common;
using GameOrg.Api.Features.Social;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Reputation;

/// <summary>
/// Выдача ачивок (docs/PLAN.md, Шаг 16). Единая точка проверки — CheckAndAwardAsync,
/// зовётся из RatingService.ApplyResultAsync для каждого участника события, сразу
/// после того, как его SportRating/ReliabilityStat/EventResult.MvpUserId уже свежие.
/// UserAchievements — источник истины, не кэш: перепроверяем все условия на каждый вызов.
/// </summary>
public sealed class AchievementService(GameOrgDbContext db, NotificationSender notificationSender, ActivityService activityService)
{
    public async Task CheckAndAwardAsync(Guid userId, Guid? eventId, CancellationToken ct)
    {
        var earnedCodes = await db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Achievement.Code)
            .ToListAsync(ct);
        var earnedSet = earnedCodes.ToHashSet();

        var totalGames = await db.SportRatings.Where(r => r.UserId == userId).SumAsync(r => (int?)r.GamesPlayed, ct) ?? 0;
        var sportsPlayed = await db.SportRatings.CountAsync(r => r.UserId == userId && r.GamesPlayed > 0, ct);
        var reliability = await db.ReliabilityStats.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        var isMvpOfThisEvent = eventId is not null && await db.EventResults.AnyAsync(r => r.EventId == eventId && r.MvpUserId == userId, ct);

        var toAward = new List<string>();
        void Consider(string code, bool condition)
        {
            if (condition && !earnedSet.Contains(code)) toAward.Add(code);
        }

        Consider("FIRST_GAME", totalGames >= 1);
        Consider("TEN_GAMES", totalGames >= 10);
        Consider("FIFTY_GAMES", totalGames >= 50);
        Consider("IRON_MAN", reliability is { CurrentStreak: >= 4 });
        Consider("MVP_FIRST", isMvpOfThisEvent);
        // 20 явок и ни одного пропуска за всё время — не скользящее окно, в схеме
        // нет истории по датам для честного "20 подряд" (тот же компромисс, что
        // ReliabilityCalculator.ComputeScore, Шаг 15).
        Consider("RELIABLE", reliability is { Attended: >= 20, NoShows: 0 });
        // Реально сыграл ≥1 игру в 2+ видах спорта, не просто добавил вид в профиль.
        Consider("MULTI_SPORT", sportsPlayed >= 2);

        if (toAward.Count == 0) return;

        var achievements = await db.Achievements.Where(a => a.IsActive && toAward.Contains(a.Code)).ToDictionaryAsync(a => a.Code, ct);
        var profileVisibility = await db.Users.Where(u => u.Id == userId).Select(u => u.ProfileVisibility).FirstAsync(ct);

        foreach (var code in toAward)
        {
            if (!achievements.TryGetValue(code, out var achievement)) continue;

            db.UserAchievements.Add(new UserAchievement
            {
                UserId = userId,
                AchievementId = achievement.Id,
                Context = eventId is null ? null : new Dictionary<string, object> { ["eventId"] = eventId.ToString()! },
            });

            // Та же гейт-логика, что AddedSport в Шаге 13 — приватный профиль в ленту не попадает.
            if (profileVisibility == Visibility.Public)
            {
                await activityService.EmitAsync(
                    userId, ActivityVerb.EarnedAchievement, eventId, null, null, null, ct,
                    new Dictionary<string, object> { ["achievementCode"] = code });
            }

            await notificationSender.SendAsync(
                userId, NotificationType.AchievementEarned,
                $"ACHIEVEMENT_EARNED:{userId}:{code}",
                "Новое достижение разблокировано!",
                new Dictionary<string, object> { ["achievementCode"] = code }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AchievementDto>> GetMyAchievementsAsync(Guid userId, string locale, CancellationToken ct)
    {
        var achievements = await db.Achievements.Where(a => a.IsActive).OrderBy(a => a.Tier).ThenBy(a => a.Code).ToListAsync(ct);
        var earned = await db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => (DateTime?)ua.EarnedAt, ct);

        return achievements.Select(a => new AchievementDto(
            a.Code, Localized.Resolve(a.NameI18n, locale) ?? a.Code, Localized.Resolve(a.DescI18n, locale), a.Icon, a.Tier,
            earned.GetValueOrDefault(a.Id))).ToList();
    }
}
