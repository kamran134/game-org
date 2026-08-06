using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Sports;

public sealed record SportDto(
    Guid Id,
    string Slug,
    Dictionary<string, string> NameI18n,
    string? Emoji,
    bool HasPositions,
    bool IsTeamSport);

public static class SportsEndpoints
{
    public static IEndpointRouteBuilder MapSportsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sports", async (GameOrgDbContext db, CancellationToken ct) =>
        {
            var sports = await db.Sports
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new SportDto(s.Id, s.Slug, s.NameI18n, s.Emoji, s.HasPositions, s.IsTeamSport))
                .ToListAsync(ct);

            return Results.Ok(sports);
        })
        .WithName("GetSports")
        .WithTags("Sports")
        .Produces<List<SportDto>>();

        return app;
    }
}
