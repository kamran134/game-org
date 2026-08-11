using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Clubs;

public static class ClubsEndpoints
{
    public static IEndpointRouteBuilder MapClubsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/clubs", async (
            HttpContext ctx, ClaimsPrincipal principal, Guid? cityId, Guid? sportId, ClubService clubService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var clubs = await clubService.GetListAsync(locale, GetUserId(principal), cityId, sportId, ct);
            return Results.Ok(clubs);
        })
        .WithName("GetClubs")
        .WithTags("Clubs")
        .Produces<List<ClubDto>>();

        app.MapGet("/api/clubs/{slug}", async (string slug, HttpContext ctx, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var club = await clubService.GetBySlugAsync(slug, locale, GetUserId(principal), ct);
            return club is null ? Results.NotFound() : Results.Ok(club);
        })
        .WithName("GetClub")
        .WithTags("Clubs")
        .Produces<ClubDetailDto>()
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clubs", async (
            CreateClubRequest request, ClaimsPrincipal principal, HttpContext ctx, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (club, error) = await clubService.CreateAsync(userId.Value, request, ct);
            if (error is not null) return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var detail = await clubService.GetBySlugAsync(club!.Slug, locale, userId, ct);
            return Results.Created($"/api/clubs/{club.Slug}", detail);
        })
        .WithName("CreateClub")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces<ClubDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapPatch("/api/clubs/{id:guid}", async (
            Guid id, UpdateClubRequest request, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.UpdateAsync(id, userId.Value, request, ct);
            if (!ok)
            {
                var status = error switch
                {
                    "Клуб не найден." => StatusCodes.Status404NotFound,
                    "Редактировать может только владелец или админ клуба." => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status400BadRequest,
                };
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("UpdateClub")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/clubs/{id:guid}", async (Guid id, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.DeleteAsync(id, userId.Value, ct);
            if (!ok)
            {
                var status = error == "Клуб не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("DeleteClub")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
