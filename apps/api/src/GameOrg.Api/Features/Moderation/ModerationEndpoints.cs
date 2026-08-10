using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Moderation;

public static class ModerationEndpoints
{
    public static IEndpointRouteBuilder MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports", async (
            CreateReportRequest request, ClaimsPrincipal principal, ReportService reportService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (created, error) = await reportService.CreateAsync(userId.Value, request, ct);
            if (error is not null)
            {
                var status = error.EndsWith("не найдена.") || error.EndsWith("не найден.") ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Created($"/api/moderation/reports/{created!.Id}", new { created.Id });
        })
        .WithName("CreateReport")
        .WithTags("Moderation")
        .RequireAuthorization()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/moderation/reports", async (HttpContext ctx, ReportService reportService, CancellationToken ct) =>
        {
            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            return Results.Ok(await reportService.GetQueueAsync(locale, ct));
        })
        .WithName("GetReportQueue")
        .WithTags("Moderation")
        .RequireAuthorization("Moderator")
        .Produces<List<ReportDto>>();

        app.MapPost("/api/moderation/reports/{id:guid}/resolve", async (
            Guid id, ResolveReportRequest request, ClaimsPrincipal principal, ReportService reportService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await reportService.ResolveAsync(id, userId.Value, request, ct);
            if (!ok)
            {
                var status = error == "Жалоба не найдена." ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("ResolveReport")
        .WithTags("Moderation")
        .RequireAuthorization("Moderator")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/moderation/users/{id:guid}/ban", async (Guid id, ClaimsPrincipal principal, ReportService reportService, CancellationToken ct) =>
            await SetBanAsync(id, principal, reportService, banned: true, ct))
        .WithName("BanUser")
        .WithTags("Moderation")
        .RequireAuthorization("Moderator")
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/moderation/users/{id:guid}/unban", async (Guid id, ClaimsPrincipal principal, ReportService reportService, CancellationToken ct) =>
            await SetBanAsync(id, principal, reportService, banned: false, ct))
        .WithName("UnbanUser")
        .WithTags("Moderation")
        .RequireAuthorization("Moderator")
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> SetBanAsync(Guid id, ClaimsPrincipal principal, ReportService reportService, bool banned, CancellationToken ct)
    {
        var userId = GetUserId(principal);
        if (userId is null) return Results.Unauthorized();

        var (ok, error) = await reportService.SetUserBanAsync(id, userId.Value, GetRole(principal) == UserRole.Admin, banned, ct);
        if (!ok)
        {
            var status = error == "Пользователь не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
            return Results.Problem(error, statusCode: status);
        }

        return Results.NoContent();
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static UserRole GetRole(ClaimsPrincipal principal)
    {
        var role = principal.FindFirstValue("role");
        return Enum.TryParse<UserRole>(role, out var parsed) ? parsed : UserRole.User;
    }
}
