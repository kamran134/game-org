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

        return app;
    }
}
