using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GameOrg.Api.Features.Venues;

public sealed record VenueDto(
    Guid Id,
    string Slug,
    string Name,
    string? Address,
    double Lat,
    double Lng,
    decimal? RatingAvg,
    int RatingCount,
    double? DistanceMeters);

public static class VenuesEndpoints
{
    public static IEndpointRouteBuilder MapVenuesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/venues", async (
            GameOrgDbContext db,
            Guid? cityId,
            Guid? sportId,
            double? lat,
            double? lng,
            double radiusKm = 10,
            CancellationToken ct = default) =>
        {
            if (radiusKm <= 0) radiusKm = 10;

            var query = db.Venues.AsQueryable();

            if (cityId is not null)
                query = query.Where(v => v.CityId == cityId);

            if (sportId is not null)
                query = query.Where(v => v.Sports.Any(vs => vs.SportId == sportId));

            Point? origin = null;
            if (lat is not null && lng is not null)
            {
                // X = долгота, Y = широта.
                origin = new Point(lng.Value, lat.Value) { SRID = 4326 };
                var radiusMeters = radiusKm * 1000;
                query = query.Where(v => v.Location.IsWithinDistance(origin, radiusMeters));
            }

            // Порядок задаётся на сущности до проекции в DTO — так EF Core надёжно
            // транслирует ORDER BY в SQL. Проекция (с повторным Distance) — последним шагом.
            query = origin is not null
                ? query.OrderBy(v => v.Location.Distance(origin))
                : query.OrderByDescending(v => v.RatingAvg);

            // ST_X/ST_Y в PostGIS определены только для geometry, не для geography (наша
            // колонка). Поэтому Location целиком материализуется как NTS Point, а X/Y
            // читаются уже в памяти — без попытки транслировать их в SQL.
            var raw = await query
                .Take(50)
                .Select(v => new
                {
                    v.Id,
                    v.Slug,
                    v.Name,
                    v.Address,
                    v.Location,
                    v.RatingAvg,
                    v.RatingCount,
                    DistanceMeters = origin == null ? (double?)null : v.Location.Distance(origin),
                })
                .ToListAsync(ct);

            var venues = raw.Select(v => new VenueDto(
                v.Id, v.Slug, v.Name, v.Address,
                v.Location.Y, v.Location.X,
                v.RatingAvg, v.RatingCount, v.DistanceMeters));

            return Results.Ok(venues);
        })
        .WithName("GetVenues")
        .WithTags("Venues")
        .Produces<List<VenueDto>>();

        return app;
    }
}
