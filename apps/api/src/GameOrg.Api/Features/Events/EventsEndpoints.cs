using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Events;

public static class EventsEndpoints
{
    public static IEndpointRouteBuilder MapEventsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events", async (
            EventService eventService,
            HttpContext ctx,
            Guid? sportId,
            Guid? cityId,
            bool upcoming,
            CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var events = await eventService.GetListAsync(sportId, cityId, upcoming, locale, ct);
            return Results.Ok(events);
        })
        .WithName("GetEvents")
        .WithTags("Events")
        .Produces<List<EventDto>>();

        app.MapGet("/api/events/{publicId}", async (string publicId, HttpContext ctx, EventService eventService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var ev = await eventService.GetByPublicIdAsync(publicId, locale, ct);
            return ev is null ? Results.NotFound() : Results.Ok(ev);
        })
        .WithName("GetEvent")
        .WithTags("Events")
        .Produces<EventDetailDto>()
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/events", async (
            CreateEventRequest request,
            ClaimsPrincipal principal,
            HttpContext ctx,
            EventService eventService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (created, error) = await eventService.CreateAsync(userId.Value, request, ct);
            if (error is not null) return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var detail = await eventService.GetByPublicIdAsync(created!.PublicId, locale, ct);
            return Results.Created($"/api/events/{created.PublicId}", detail);
        })
        .WithName("CreateEvent")
        .WithTags("Events")
        .RequireAuthorization()
        .Produces<EventDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapPatch("/api/events/{id:guid}", async (
            Guid id,
            UpdateEventRequest request,
            ClaimsPrincipal principal,
            EventService eventService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await eventService.UpdateAsync(id, userId.Value, request, ct);
            if (!ok)
            {
                var status = error == "Событие не найдено."
                    ? StatusCodes.Status404NotFound
                    : error == "Редактировать может только создатель."
                        ? StatusCodes.Status403Forbidden
                        : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("UpdateEvent")
        .WithTags("Events")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/events/{id:guid}/participants", async (
            Guid id,
            JoinEventRequest request,
            ClaimsPrincipal principal,
            HttpContext ctx,
            EventService eventService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var (result, error) = await eventService.JoinAsync(id, userId.Value, request, locale, ct);
            if (error is not null)
            {
                var status = error == "Событие не найдено." ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Created($"/api/events/{id}/participants/{result!.Id}", result);
        })
        .WithName("JoinEvent")
        .WithTags("Events")
        .RequireAuthorization()
        .Produces<EventParticipantDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/events/{id:guid}/participants/me", async (
            Guid id,
            ClaimsPrincipal principal,
            EventService eventService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var removed = await eventService.LeaveAsync(id, userId.Value, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        })
        .WithName("LeaveEvent")
        .WithTags("Events")
        .RequireAuthorization()
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/events/{id:guid}/cancel", async (
            Guid id,
            CancelEventRequest request,
            ClaimsPrincipal principal,
            EventService eventService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await eventService.CancelAsync(id, userId.Value, request.Reason, ct);
            if (!ok)
            {
                var status = error == "Событие не найдено."
                    ? StatusCodes.Status404NotFound
                    : error == "Отменить может только создатель."
                        ? StatusCodes.Status403Forbidden
                        : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("CancelEvent")
        .WithTags("Events")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/events/{id:guid}/guests", async (
            Guid id,
            AddGuestRequest request,
            ClaimsPrincipal principal,
            EventService eventService,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (result, error) = await eventService.AddGuestAsync(id, userId.Value, request, ct);
            if (error is not null)
            {
                var status = error == "Событие не найдено." ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Created($"/api/events/{id}/participants/{result!.Id}", result);
        })
        .WithName("AddEventGuest")
        .WithTags("Events")
        .RequireAuthorization()
        .Produces<EventParticipantDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
