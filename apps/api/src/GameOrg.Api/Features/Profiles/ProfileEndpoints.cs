using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Api.Features.Geography;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Profiles;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", async (ClaimsPrincipal principal, HttpContext ctx, GameOrgDbContext db, CancellationToken ct) =>
        {
            var user = await LoadFullUserAsync(db, principal, ct);
            if (user is null) return Results.Unauthorized();

            var locale = RequestLocale.ResolveAndVary(ctx);
            return Results.Ok(MapMe(user, locale));
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
            return Results.Ok(MapMe(user, locale));
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
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (userSport, error) = await profileService.UpsertSportAsync(userId.Value, sportId, request, ct);
            return error is not null
                ? Results.Problem(error, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(MapUserSport(userSport!));
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

        app.MapGet("/api/users/{handle}", async (string handle, HttpContext ctx, GameOrgDbContext db, CancellationToken ct) =>
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
            return Results.Ok(MapPublic(user, locale));
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

    private static MeProfileDto MapMe(User user, string locale) => new(
        user.Id, user.Handle,
        Localized.Resolve(user.DisplayNameI18n, locale) ?? "",
        Localized.Resolve(user.BioI18n, locale),
        user.BirthDate, user.Gender, user.Phone,
        user.Locale, user.Timezone, user.ProfileVisibility, MapCity(user.City), user.AvatarId, user.IsVerified,
        user.Sports.Select(MapUserSport).ToList(),
        LocalizedTextDto.From(user.DisplayNameI18n),
        LocalizedTextDto.FromNullable(user.BioI18n));

    private static PublicProfileDto MapPublic(User user, string locale) => new(
        user.Handle,
        Localized.Resolve(user.DisplayNameI18n, locale) ?? "",
        Localized.Resolve(user.BioI18n, locale),
        MapCity(user.City), user.AvatarId, user.IsVerified,
        user.Sports.Where(s => s.Visibility == Visibility.Public).Select(MapPublicUserSport).ToList());

    private static CityDto? MapCity(City? city) => city is null ? null : new CityDto(city.Id, city.Slug, city.NameI18n, city.Lat, city.Lng);

    private static UserSportDto MapUserSport(UserSport s) => new(
        s.SportId, s.Sport.Slug, s.Sport.Emoji, s.Level, s.IsPrimary, s.PlayingSince, s.Footedness,
        s.HeightCm, s.JerseyNumber, s.Note, s.Visibility, MapPositions(s));

    private static PublicUserSportDto MapPublicUserSport(UserSport s) => new(
        s.Sport.Slug, s.Sport.Emoji, s.Level, s.IsPrimary, s.PlayingSince, s.Footedness,
        s.HeightCm, s.JerseyNumber, MapPositions(s));

    private static List<UserSportPositionDto> MapPositions(UserSport s) =>
        s.Positions.Select(p => new UserSportPositionDto(p.PositionId, p.Position.Code, p.IsPrimary)).ToList();
}
