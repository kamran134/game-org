using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Domain;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GameOrg.Api.Features.Venues;

public static class VenuesEndpoints
{
    public static IEndpointRouteBuilder MapVenuesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/venues", async (
            GameOrgDbContext db,
            HttpContext ctx,
            Guid? cityId,
            Guid? sportId,
            double? lat,
            double? lng,
            double radiusKm = 10,
            CancellationToken ct = default) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            if (radiusKm <= 0) radiusKm = 10;

            // Только Published — премодерация (docs/PLAN.md, Шаг 9). Свои
            // Draft-площадки автор видит через GetBySlugAsync напрямую по
            // ссылке, в общий список они не попадают, пока не пройдут проверку.
            var query = db.Venues.Where(v => v.Status == VenueStatus.Published);

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
                    v.NameI18n,
                    v.AddressI18n,
                    v.Location,
                    v.RatingAvg,
                    v.RatingCount,
                    DistanceMeters = origin == null ? (double?)null : v.Location.Distance(origin),
                })
                .ToListAsync(ct);

            var venues = raw.Select(v => new VenueDto(
                v.Id, v.Slug,
                Localized.Resolve(v.NameI18n, locale) ?? "",
                Localized.Resolve(v.AddressI18n, locale),
                v.Location.Y, v.Location.X,
                v.RatingAvg, v.RatingCount, v.DistanceMeters));

            return Results.Ok(venues);
        })
        .WithName("GetVenues")
        .WithTags("Venues")
        .Produces<List<VenueDto>>();

        app.MapGet("/api/venues/{slug}", async (string slug, HttpContext ctx, ClaimsPrincipal principal, VenueService venueService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            // Анонимный эндпоинт, но principal всё равно заполнен, если в запросе
            // был валидный JWT (UseAuthentication работает независимо от
            // RequireAuthorization на конкретном маршруте) — нужно знать, кто
            // смотрит, чтобы решить, можно ли ему чужой Draft.
            var venue = await venueService.GetBySlugAsync(slug, locale, GetUserId(principal), IsModeratorOrAdmin(principal), ct);
            return venue is null ? Results.NotFound() : Results.Ok(venue);
        })
        .WithName("GetVenue")
        .WithTags("Venues")
        .Produces<VenueDetailDto>()
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/venues", async (
            CreateVenueRequest request,
            ClaimsPrincipal principal,
            HttpContext ctx,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var (venue, error) = await venueService.CreateAsync(userId.Value, GetRole(principal), request, ct);
            if (error is not null) return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

            // Автор своей же Draft-площадки — viewerId совпадает с CreatedById,
            // GetBySlugAsync её покажет несмотря на статус.
            var detail = await venueService.GetBySlugAsync(venue!.Slug, locale, userId, IsModeratorOrAdmin(principal), ct);
            return Results.Created($"/api/venues/{venue.Slug}", detail);
        })
        .WithName("CreateVenue")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces<VenueDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapPatch("/api/venues/{id:guid}", async (
            Guid id,
            UpdateVenueRequest request,
            ClaimsPrincipal principal,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await venueService.UpdateAsync(id, userId.Value, request, ct);
            if (!ok)
            {
                // Форбидден — только несовпадение владельца; всё остальное
                // (не найдена / провалилась валидация полей) — 404/400.
                var status = error switch
                {
                    "Площадка не найдена." => StatusCodes.Status404NotFound,
                    "Редактировать может только создатель." => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status400BadRequest,
                };
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("UpdateVenue")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/venues/{id:guid}", async (Guid id, ClaimsPrincipal principal, VenueService venueService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await venueService.DeleteAsync(id, userId.Value, IsModeratorOrAdmin(principal), ct);
            if (!ok)
            {
                var status = error == "Площадка не найдена." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("DeleteVenue")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/venues/{id:guid}/photos/presign", async (
            Guid id,
            PresignPhotoRequest request,
            ClaimsPrincipal principal,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (result, error) = await venueService.PresignPhotoAsync(id, userId.Value, request, ct);
            return error is not null
                ? Results.Problem(error, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(result);
        })
        .WithName("PresignVenuePhoto")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces<PresignPhotoResponse>()
        .Produces(StatusCodes.Status400BadRequest);

        app.MapPost("/api/venues/{id:guid}/photos", async (
            Guid id,
            AttachPhotoRequest request,
            ClaimsPrincipal principal,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (result, error) = await venueService.AttachPhotoAsync(id, userId.Value, request, ct);
            return error is not null
                ? Results.Problem(error, statusCode: StatusCodes.Status400BadRequest)
                : Results.Ok(result);
        })
        .WithName("AttachVenuePhoto")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces<VenuePhotoDto>()
        .Produces(StatusCodes.Status400BadRequest);

        app.MapDelete("/api/venues/{id:guid}/photos/{photoId:guid}", async (
            Guid id,
            Guid photoId,
            ClaimsPrincipal principal,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var removed = await venueService.RemovePhotoAsync(id, photoId, userId.Value, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveVenuePhoto")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/venues/{id:guid}/reviews", async (
            Guid id,
            int skip,
            int take,
            HttpContext ctx,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var reviews = await venueService.GetReviewsAsync(id, skip, take == 0 ? 20 : take, locale, ct);
            return Results.Ok(reviews);
        })
        .WithName("GetVenueReviews")
        .WithTags("Venues")
        .Produces<List<VenueReviewDto>>();

        app.MapPost("/api/venues/{id:guid}/reviews", async (
            Guid id,
            UpsertReviewRequest request,
            ClaimsPrincipal principal,
            HttpContext ctx,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var (result, error, conflict) = await venueService.CreateReviewAsync(id, userId.Value, request, locale, ct);
            if (error is not null)
            {
                var status = conflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Created($"/api/venues/{id}/reviews/{result!.Id}", result);
        })
        .WithName("CreateVenueReview")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces<VenueReviewDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status409Conflict);

        app.MapPatch("/api/venues/{id:guid}/reviews/{reviewId:guid}", async (
            Guid id,
            Guid reviewId,
            UpsertReviewRequest request,
            ClaimsPrincipal principal,
            HttpContext ctx,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var (result, error) = await venueService.UpdateReviewAsync(id, reviewId, userId.Value, request, locale, ct);
            if (error is not null)
            {
                var status = error is "Отзыв не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Ok(result);
        })
        .WithName("UpdateVenueReview")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces<VenueReviewDto>()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/venues/{id:guid}/reviews/{reviewId:guid}", async (
            Guid id,
            Guid reviewId,
            ClaimsPrincipal principal,
            VenueService venueService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var removed = await venueService.RemoveReviewAsync(id, reviewId, userId.Value, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveVenueReview")
        .WithTags("Venues")
        .RequireAuthorization()
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/moderation/venues", async (HttpContext ctx, VenueService venueService, CancellationToken ct) =>
        {
            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            return Results.Ok(await venueService.GetModerationQueueAsync(locale, ct));
        })
        .WithName("GetVenueModerationQueue")
        .WithTags("Venues")
        .RequireAuthorization("Moderator")
        .Produces<List<VenueDto>>();

        app.MapPost("/api/venues/{id:guid}/publish", async (Guid id, ClaimsPrincipal principal, VenueService venueService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await venueService.SetStatusAsync(id, userId.Value, VenueStatus.Published, ct);
            return ok ? Results.NoContent() : Results.NotFound(error);
        })
        .WithName("PublishVenue")
        .WithTags("Venues")
        .RequireAuthorization("Moderator")
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/venues/{id:guid}/hide", async (Guid id, ClaimsPrincipal principal, VenueService venueService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await venueService.SetStatusAsync(id, userId.Value, VenueStatus.Hidden, ct);
            return ok ? Results.NoContent() : Results.NotFound(error);
        })
        .WithName("HideVenue")
        .WithTags("Venues")
        .RequireAuthorization("Moderator")
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static UserRole GetRole(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue("role");
        return Enum.TryParse<UserRole>(role, out var parsed) ? parsed : UserRole.User;
    }

    private static bool IsModeratorOrAdmin(ClaimsPrincipal principal) =>
        GetRole(principal) is UserRole.Moderator or UserRole.Admin;

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
