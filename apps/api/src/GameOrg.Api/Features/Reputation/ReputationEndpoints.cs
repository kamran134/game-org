using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Reputation;

public static class ReputationEndpoints
{
    public static IEndpointRouteBuilder MapReputationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sports/{slug}/leaderboard", async (string slug, int? take, HttpContext ctx, RatingService ratingService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var result = await ratingService.GetLeaderboardAsync(slug, locale, take ?? 50, ct);
            return Results.Ok(result);
        })
        .WithName("GetSportLeaderboard")
        .WithTags("Reputation")
        .Produces<List<LeaderboardEntryDto>>();

        app.MapGet("/api/me/achievements", async (ClaimsPrincipal principal, HttpContext ctx, AchievementService achievementService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.ResolveAndVary(ctx);
            var result = await achievementService.GetMyAchievementsAsync(userId.Value, locale, ct);
            return Results.Ok(result);
        })
        .WithName("GetMyAchievements")
        .WithTags("Reputation")
        .RequireAuthorization()
        .Produces<List<AchievementDto>>();

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
