using GameOrg.Api.Common;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Social;

/// <summary>
/// Лента — fan-out on read (docs/PLAN.md §10). Приватность решается на записи:
/// вызывающий код (EventService/ClubService/VenueService/ProfileService) зовёт
/// EmitAsync, только если сущность-повод публично видна — сюда долетают уже
/// отфильтрованные события, Audience здесь всегда Public.
/// </summary>
public sealed class ActivityService(GameOrgDbContext db, FollowService followService)
{
    public async Task EmitAsync(
        Guid actorId, ActivityVerb verb, Guid? eventId, Guid? clubId, Guid? venueId, Guid? targetUserId, CancellationToken ct)
    {
        db.Activities.Add(new Activity
        {
            ActorId = actorId,
            Verb = verb,
            EventId = eventId,
            ClubId = clubId,
            VenueId = venueId,
            TargetUserId = targetUserId,
            Audience = Visibility.Public,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<ActivityDto>> GetFeedAsync(Guid userId, string locale, int skip, int take, CancellationToken ct)
    {
        var followedUserIds = await followService.GetFollowedUserIdsAsync(userId, ct);
        var followedClubIds = await followService.GetFollowedClubIdsAsync(userId, ct);
        var followedVenueIds = await followService.GetFollowedVenueIdsAsync(userId, ct);

        var query = db.Activities.Where(a =>
            a.Audience == Visibility.Public &&
            (followedUserIds.Contains(a.ActorId)
                || (a.ClubId != null && followedClubIds.Contains(a.ClubId.Value))
                || (a.VenueId != null && followedVenueIds.Contains(a.VenueId.Value))));

        var raw = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 50))
            .Select(a => new
            {
                a.Id,
                a.Verb,
                a.CreatedAt,
                ActorId = a.Actor.Id,
                a.Actor.Handle,
                a.Actor.DisplayNameI18n,
                a.Actor.AvatarId,
                a.EventId,
                EventPublicId = a.Event != null ? a.Event.PublicId : null,
                EventTitle = a.Event != null ? a.Event.TitleI18n : null,
                EventStartsAt = a.Event != null ? a.Event.StartsAt : (DateTime?)null,
                a.ClubId,
                ClubSlug = a.Club != null ? a.Club.Slug : null,
                ClubName = a.Club != null ? a.Club.NameI18n : null,
                a.VenueId,
                VenueSlug = a.Venue != null ? a.Venue.Slug : null,
                VenueName = a.Venue != null ? a.Venue.NameI18n : null,
                a.TargetUserId,
                TargetUserHandle = a.TargetUser != null ? a.TargetUser.Handle : null,
                TargetUserName = a.TargetUser != null ? a.TargetUser.DisplayNameI18n : null,
                TargetUserAvatarId = a.TargetUser != null ? a.TargetUser.AvatarId : (Guid?)null,
            })
            .ToListAsync(ct);

        return raw.Select(a => new ActivityDto(
            a.Id,
            new ActivityActorDto(a.ActorId, a.Handle, Localized.Resolve(a.DisplayNameI18n, locale) ?? a.Handle, a.AvatarId),
            a.Verb,
            a.EventId is null ? null : new ActivityEventDto(a.EventId.Value, a.EventPublicId!, Localized.Resolve(a.EventTitle, locale), a.EventStartsAt!.Value),
            a.ClubId is null ? null : new ActivityClubDto(a.ClubId.Value, a.ClubSlug!, Localized.Resolve(a.ClubName, locale) ?? a.ClubSlug!),
            a.VenueId is null ? null : new ActivityVenueDto(a.VenueId.Value, a.VenueSlug!, Localized.Resolve(a.VenueName, locale) ?? a.VenueSlug!),
            a.TargetUserId is null ? null : new ActivityActorDto(
                a.TargetUserId.Value, a.TargetUserHandle!, Localized.Resolve(a.TargetUserName, locale) ?? a.TargetUserHandle!, a.TargetUserAvatarId),
            a.CreatedAt)).ToList();
    }
}
