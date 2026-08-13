using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Moderation;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/overview", async (AdminService adminService, CancellationToken ct) =>
            Results.Ok(await adminService.GetOverviewAsync(ct)))
        .WithName("GetAdminOverview")
        .WithTags("Admin")
        .RequireAuthorization("Moderator")
        .Produces<AdminOverviewDto>();

        app.MapGet("/api/admin/users", async (
            HttpContext ctx,
            AdminService adminService,
            string? query,
            UserRole? role,
            bool? banned,
            CancellationToken ct,
            int skip = 0,
            int take = 20) =>
        {
            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var users = await adminService.SearchUsersAsync(query, role, banned, locale, skip, take, ct);
            return Results.Ok(users);
        })
        .WithName("SearchAdminUsers")
        .WithTags("Admin")
        .RequireAuthorization("Moderator")
        .Produces<List<AdminUserDto>>();

        app.MapPatch("/api/admin/users/{id:guid}/role", async (
            Guid id, UpdateUserRoleRequest request, ClaimsPrincipal principal, AdminService adminService, CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (actorId is null) return Results.Unauthorized();

            var (ok, error) = await adminService.UpdateRoleAsync(id, actorId.Value, request.Role, ct);
            if (!ok)
            {
                var status = error == "Пользователь не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("UpdateUserRole")
        .WithTags("Admin")
        .RequireAuthorization("Admin")
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
