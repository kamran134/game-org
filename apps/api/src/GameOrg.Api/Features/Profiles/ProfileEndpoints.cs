using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Reputation;
using GameOrg.Api.Features.Social;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Profiles;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (
            ClaimsPrincipal principal, HttpContext ctx, GameOrgDbContext db, FollowService followService, AchievementService achievementService, CancellationToken ct) =>
        {
            var user = await LoadFullUserAsync(db, principal, ct);
            if (user is null) return Results.Unauthorized();

            var locale = RequestLocale.ResolveAndVary(ctx);
            var followersCount = await followService.CountFollowersAsync(FollowTargetType.User, user.Id, ct);
            var followingCount = await followService.CountFollowingAsync(user.Id, ct);
            var (ratings, reliabilityScore) = await GetReputationAsync(db, user.Id, ct);
            var achievements = await GetEarnedAchievementsAsync(achievementService, user.Id, locale, ct);
            return Results.Ok(MapMe(user, locale, followersCount, followingCount, reliabilityScore, ratings, achievements));
        })
        .WithName("Me")
        .WithTags("Profiles")
        .RequireAuthorization()
        .Produces<MeProfileDto>()
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapPatch("/api/me", async (
            UpdateMeRequest request,
            ClaimsPrincipal principal,
            HttpContext ctx,
            GameOrgDbContext db,
            ProfileService profileService,
            FollowService followService,
            AchievementService achievementService,
            CancellationToken ct) =>
        {
            var user = await LoadFullUserAsync(db, principal, ct);
            if (user is null) return Results.Unauthorized();

            var (error, conflict) = await profileService.UpdateMeAsync(user, request, ct);
            if (error is not null)
            {
                var status = conflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var followersCount = await followService.CountFollowersAsync(FollowTargetType.User, user.Id, ct);
            var followingCount = await followService.CountFollowingAsync(user.Id, ct);
            var (ratings, reliabilityScore) = await GetReputationAsync(db, user.Id, ct);
            var achievements = await GetEarnedAchievementsAsync(achievementService, user.Id, locale, ct);
            return Results.Ok(MapMe(user, locale, followersCount, followingCount, reliabilityScore, ratings, achievements));
        })
        .WithName("UpdateMe")
        .WithTags("Profiles")
        .RequireAuthorization()
        .Produces<MeProfileDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);

        app.MapPut("/api/me/sports/{sportId:guid}", async (
            Guid sportId,
            UpsertUserSportRequest request,
            ClaimsPrincipal principal,
            ProfileService profileService,
            GameOrgDbContext db,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (userSport, error) = await profileService.UpsertSportAsync(userId.Value, sportId, request, ct);
            if (error is not null) return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

            var (ratings, _) = await GetReputationAsync(db, userId.Value, ct);
            return Results.Ok(MapUserSport(userSport!, ratings));
        })
        .WithName("UpsertMySport")
        .WithTags("Profiles")
        .RequireAuthorization()
        .Produces<UserSportDto>()
        .Produces(StatusCodes.Status400BadRequest);

        app.MapDelete("/api/me/sports/{sportId:guid}", async (
            Guid sportId,
            ClaimsPrincipal principal,
            ProfileService profileService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var removed = await profileService.RemoveSportAsync(userId.Value, sportId, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveMySport")
        .WithTags("Profiles")
        .RequireAuthorization()
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/users/{handle}", async (
            string handle, HttpContext ctx, ClaimsPrincipal principal, GameOrgDbContext db, FollowService followService, AchievementService achievementService, CancellationToken ct) =>
        {
            var normalized = handle.TrimStart('@').ToLowerInvariant();

            var user = await db.Users
                .Include(u => u.City)
                .Include(u => u.Sports).ThenInclude(s => s.Sport)
                .Include(u => u.Sports).ThenInclude(s => s.Positions).ThenInclude(p => p.Position)
                .FirstOrDefaultAsync(u => u.Handle == normalized && u.Status == UserStatus.Active, ct);

            if (user is null || user.ProfileVisibility != Visibility.Public)
                return Results.NotFound();

            var locale = RequestLocale.ResolveAndVary(ctx);
            var followersCount = await followService.CountFollowersAsync(FollowTargetType.User, user.Id, ct);
            var viewerIsFollowing = await followService.IsFollowingAsync(GetUserId(principal), FollowTargetType.User, user.Id, ct);
            var (ratings, reliabilityScore) = await GetReputationAsync(db, user.Id, ct);
            var achievements = await GetEarnedAchievementsAsync(achievementService, user.Id, locale, ct);
            return Results.Ok(MapPublic(user, locale, followersCount, viewerIsFollowing, reliabilityScore, ratings, achievements));
        })
        .WithName("GetPublicProfile")
        .WithTags("Profiles")
        .Produces<PublicProfileDto>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static async Task<User?> LoadFullUserAsync(GameOrgDbContext db, ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null) return null;

        return await db.Users
            .Include(u => u.City)
            .Include(u => u.Sports).ThenInclude(s => s.Sport)
            .Include(u => u.Sports).ThenInclude(s => s.Positions).ThenInclude(p => p.Position)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
    }

    /// <summary>SportRating по каждому виду спорта пользователя + ReliabilityStat.Score (100 — нет истории).</summary>
    private static async Task<(Dictionary<Guid, SportRating> Ratings, int ReliabilityScore)> GetReputationAsync(GameOrgDbContext db, Guid userId, CancellationToken ct)
    {
        var ratings = await db.SportRatings.Where(r => r.UserId == userId).ToDictionaryAsync(r => r.SportId, ct);
        var score = await db.ReliabilityStats.Where(s => s.UserId == userId).Select(s => (int?)s.Score).FirstOrDefaultAsync(ct) ?? 100;
        return (ratings, score);
    }

    private static async Task<List<AchievementDto>> GetEarnedAchievementsAsync(AchievementService achievementService, Guid userId, string locale, CancellationToken ct)
    {
        var catalog = await achievementService.GetMyAchievementsAsync(userId, locale, ct);
        return catalog.Where(a => a.EarnedAt is not null).ToList();
    }

    private static MeProfileDto MapMe(
        User user, string locale, int followersCount, int followingCount, int reliabilityScore, Dictionary<Guid, SportRating> ratings, List<AchievementDto> achievements) => new(
        user.Id, user.Handle,
        Localized.Resolve(user.DisplayNameI18n, locale) ?? "",
        Localized.Resolve(user.BioI18n, locale),
        user.BirthDate, user.Gender, user.Phone,
        user.Locale, user.Timezone, user.ProfileVisibility, MapCity(user.City), user.AvatarId, user.IsVerified,
        user.Role,
        user.Sports.Select(s => MapUserSport(s, ratings)).ToList(),
        followersCount, followingCount, reliabilityScore, achievements,
        LocalizedTextDto.From(user.DisplayNameI18n),
        LocalizedTextDto.FromNullable(user.BioI18n));

    private static PublicProfileDto MapPublic(
        User user, string locale, int followersCount, bool viewerIsFollowing, int reliabilityScore, Dictionary<Guid, SportRating> ratings, List<AchievementDto> achievements) => new(
        user.Id,
        user.Handle,
        Localized.Resolve(user.DisplayNameI18n, locale) ?? "",
        Localized.Resolve(user.BioI18n, locale),
        MapCity(user.City), user.AvatarId, user.IsVerified,
        user.Sports.Where(s => s.Visibility == Visibility.Public).Select(s => MapPublicUserSport(s, ratings)).ToList(),
        followersCount, viewerIsFollowing, reliabilityScore, achievements);

    private static CityDto? MapCity(City? city) => city is null ? null : new CityDto(city.Id, city.Slug, city.NameI18n, city.Lat, city.Lng);

    private static UserSportDto MapUserSport(UserSport s, Dictionary<Guid, SportRating> ratings)
    {
        ratings.TryGetValue(s.SportId, out var r);
        return new UserSportDto(
            s.SportId, s.Sport.Slug, s.Sport.Emoji, s.Level, s.IsPrimary, s.PlayingSince, s.Footedness,
            s.HeightCm, s.JerseyNumber, s.Note, s.Visibility, MapPositions(s),
            r?.Rating ?? 1500, r?.GamesPlayed ?? 0, r?.Wins ?? 0, r?.Draws ?? 0, r?.Losses ?? 0);
    }

    private static PublicUserSportDto MapPublicUserSport(UserSport s, Dictionary<Guid, SportRating> ratings)
    {
        ratings.TryGetValue(s.SportId, out var r);
        return new PublicUserSportDto(
            s.Sport.Slug, s.Sport.Emoji, s.Level, s.IsPrimary, s.PlayingSince, s.Footedness,
            s.HeightCm, s.JerseyNumber, MapPositions(s),
            r?.Rating ?? 1500, r?.GamesPlayed ?? 0, r?.Wins ?? 0, r?.Draws ?? 0, r?.Losses ?? 0);
    }

    private static List<UserSportPositionDto> MapPositions(UserSport s) =>
        s.Positions.Select(p => new UserSportPositionDto(p.PositionId, p.Position.Code, p.IsPrimary)).ToList();
}
