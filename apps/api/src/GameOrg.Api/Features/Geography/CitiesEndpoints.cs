using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Geography;

public sealed record CityDto(
    Guid Id,
    string Slug,
    Dictionary<string, string> NameI18n,
    double Lat,
    double Lng);

public static class CitiesEndpoints
{
    public static IEndpointRouteBuilder MapCitiesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cities", async (GameOrgDbContext db, CancellationToken ct) =>
        {
            var cities = await db.Cities
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new CityDto(c.Id, c.Slug, c.NameI18n, c.Lat, c.Lng))
                .ToListAsync(ct);

            return Results.Ok(cities);
        })
        .WithName("GetCities")
        .WithTags("Geography")
        .Produces<List<CityDto>>();

        return app;
    }
}
