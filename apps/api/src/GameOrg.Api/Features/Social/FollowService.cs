using GameOrg.Api.Common;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Social;

/// <summary>Подписки на User/Club/Venue поверх полиморфного Follow (три nullable FK в таблице).</summary>
public sealed class FollowService(GameOrgDbContext db, NotificationSender notificationSender)
{
    /// <summary>
    /// Подписаться можно только на публично видимую цель — на приватный клуб/Draft-площадку/
    /// непубличный профиль подписки нет вообще (не только по кнопке, но и по прямому вызову API).
    /// Уже подписан → идемпотентно ОК, не ошибка.
    /// </summary>
    public async Task<(bool Ok, string? Error)> FollowAsync(Guid followerId, FollowTargetType targetType, Guid targetId, CancellationToken ct)
    {
        var (targetOk, error) = targetType switch
        {
            FollowTargetType.User => await ValidateUserTargetAsync(followerId, targetId, ct),
            FollowTargetType.Club => await ValidateClubTargetAsync(targetId, ct),
            FollowTargetType.Venue => await ValidateVenueTargetAsync(targetId, ct),
            _ => (false, "Неизвестный тип цели."),
        };
        if (!targetOk) return (false, error);

        if (await FindAsync(followerId, targetType, targetId, ct) is not null) return (true, null);

        var follow = new Follow { FollowerId = followerId };
        switch (targetType)
        {
            case FollowTargetType.User: follow.TargetUserId = targetId; break;
            case FollowTargetType.Club: follow.TargetClubId = targetId; break;
            case FollowTargetType.Venue: follow.TargetVenueId = targetId; break;
        }

        db.Follows.Add(follow);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Гонка на unique-индексе (FollowerId, TargetXId) — уже подписан, трактуем как успех.
            if (await FindAsync(followerId, targetType, targetId, ct) is null) throw;
            return (true, null);
        }

        if (targetType == FollowTargetType.User)
        {
            await notificationSender.SendAsync(
                targetId, NotificationType.NewFollower,
                $"NEW_FOLLOWER:{targetId}:{followerId}:{DateTime.UtcNow.Ticks}",
                "У вас новый подписчик.",
                new Dictionary<string, object> { ["followerId"] = followerId.ToString() }, ct);
        }

        return (true, null);
    }

    public async Task<bool> UnfollowAsync(Guid followerId, FollowTargetType targetType, Guid targetId, CancellationToken ct)
    {
        var follow = await FindAsync(followerId, targetType, targetId, ct);
        if (follow is null) return false;

        db.Follows.Remove(follow);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> IsFollowingAsync(Guid? followerId, FollowTargetType targetType, Guid targetId, CancellationToken ct) =>
        followerId is not null && await FindAsync(followerId.Value, targetType, targetId, ct) is not null;

    public Task<int> CountFollowersAsync(FollowTargetType targetType, Guid targetId, CancellationToken ct) => targetType switch
    {
        FollowTargetType.User => db.Follows.CountAsync(f => f.TargetUserId == targetId, ct),
        FollowTargetType.Club => db.Follows.CountAsync(f => f.TargetClubId == targetId, ct),
        FollowTargetType.Venue => db.Follows.CountAsync(f => f.TargetVenueId == targetId, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
    };

    public Task<int> CountFollowingAsync(Guid userId, CancellationToken ct) =>
        db.Follows.CountAsync(f => f.FollowerId == userId, ct);

    /// <summary>Подписчики цели — всегда пользователи (FollowerId всегда User).</summary>
    public async Task<List<FollowSummaryDto>> GetFollowersAsync(
        FollowTargetType targetType, Guid targetId, string locale, int skip, int take, CancellationToken ct)
    {
        IQueryable<Follow> query = targetType switch
        {
            FollowTargetType.User => db.Follows.Where(f => f.TargetUserId == targetId),
            FollowTargetType.Club => db.Follows.Where(f => f.TargetClubId == targetId),
            FollowTargetType.Venue => db.Follows.Where(f => f.TargetVenueId == targetId),
            _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
        };

        var followers = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 50))
            .Select(f => new { f.Follower.Id, f.Follower.Handle, f.Follower.DisplayNameI18n, f.Follower.AvatarId })
            .ToListAsync(ct);

        return followers.Select(u => new FollowSummaryDto(
            FollowTargetType.User, u.Id, u.Handle, Localized.Resolve(u.DisplayNameI18n, locale) ?? u.Handle, u.AvatarId)).ToList();
    }

    /// <summary>Подписки пользователя — User/Club/Venue вперемешку, если targetType не задан.</summary>
    public async Task<List<FollowSummaryDto>> GetFollowingAsync(
        Guid userId, FollowTargetType? targetType, string locale, int skip, int take, CancellationToken ct)
    {
        var query = db.Follows.Where(f => f.FollowerId == userId);
        query = targetType switch
        {
            FollowTargetType.User => query.Where(f => f.TargetUserId != null),
            FollowTargetType.Club => query.Where(f => f.TargetClubId != null),
            FollowTargetType.Venue => query.Where(f => f.TargetVenueId != null),
            _ => query,
        };

        var rows = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 50))
            .Select(f => new
            {
                f.TargetUserId,
                TargetUserHandle = f.TargetUser != null ? f.TargetUser.Handle : null,
                TargetUserName = f.TargetUser != null ? f.TargetUser.DisplayNameI18n : null,
                TargetUserAvatarId = f.TargetUser != null ? f.TargetUser.AvatarId : (Guid?)null,
                f.TargetClubId,
                TargetClubSlug = f.TargetClub != null ? f.TargetClub.Slug : null,
                TargetClubName = f.TargetClub != null ? f.TargetClub.NameI18n : null,
                f.TargetVenueId,
                TargetVenueSlug = f.TargetVenue != null ? f.TargetVenue.Slug : null,
                TargetVenueName = f.TargetVenue != null ? f.TargetVenue.NameI18n : null,
            })
            .ToListAsync(ct);

        return rows.Select(r => r.TargetUserId is not null
            ? new FollowSummaryDto(FollowTargetType.User, r.TargetUserId.Value, r.TargetUserHandle!,
                Localized.Resolve(r.TargetUserName, locale) ?? r.TargetUserHandle!, r.TargetUserAvatarId)
            : r.TargetClubId is not null
                ? new FollowSummaryDto(FollowTargetType.Club, r.TargetClubId.Value, r.TargetClubSlug!,
                    Localized.Resolve(r.TargetClubName, locale) ?? r.TargetClubSlug!, null)
                : new FollowSummaryDto(FollowTargetType.Venue, r.TargetVenueId!.Value, r.TargetVenueSlug!,
                    Localized.Resolve(r.TargetVenueName, locale) ?? r.TargetVenueSlug!, null))
            .ToList();
    }

    /// <summary>Для ActivityService.GetFeedAsync — fan-out on read по подпискам.</summary>
    internal Task<List<Guid>> GetFollowedUserIdsAsync(Guid userId, CancellationToken ct) =>
        db.Follows.Where(f => f.FollowerId == userId && f.TargetUserId != null).Select(f => f.TargetUserId!.Value).ToListAsync(ct);

    internal Task<List<Guid>> GetFollowedClubIdsAsync(Guid userId, CancellationToken ct) =>
        db.Follows.Where(f => f.FollowerId == userId && f.TargetClubId != null).Select(f => f.TargetClubId!.Value).ToListAsync(ct);

    internal Task<List<Guid>> GetFollowedVenueIdsAsync(Guid userId, CancellationToken ct) =>
        db.Follows.Where(f => f.FollowerId == userId && f.TargetVenueId != null).Select(f => f.TargetVenueId!.Value).ToListAsync(ct);

    private Task<Follow?> FindAsync(Guid followerId, FollowTargetType targetType, Guid targetId, CancellationToken ct) => targetType switch
    {
        FollowTargetType.User => db.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.TargetUserId == targetId, ct),
        FollowTargetType.Club => db.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.TargetClubId == targetId, ct),
        FollowTargetType.Venue => db.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.TargetVenueId == targetId, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
    };

    private async Task<(bool Ok, string? Error)> ValidateUserTargetAsync(Guid followerId, Guid targetId, CancellationToken ct)
    {
        if (targetId == followerId) return (false, "Нельзя подписаться на самого себя.");

        var target = await db.Users.FirstOrDefaultAsync(u => u.Id == targetId && u.Status == UserStatus.Active, ct);
        return target is not null && target.ProfileVisibility == Visibility.Public ? (true, null) : (false, "Пользователь не найден.");
    }

    private async Task<(bool Ok, string? Error)> ValidateClubTargetAsync(Guid targetId, CancellationToken ct)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == targetId, ct);
        return club is not null && club.Visibility != ClubVisibility.Private ? (true, null) : (false, "Клуб не найден.");
    }

    private async Task<(bool Ok, string? Error)> ValidateVenueTargetAsync(Guid targetId, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == targetId, ct);
        return venue is not null && venue.Status == VenueStatus.Published ? (true, null) : (false, "Площадка не найдена.");
    }
}
