using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events/{id:guid}/payments", async (
            Guid id, HttpContext ctx, ClaimsPrincipal principal, PaymentService paymentService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.ResolveAndVary(ctx);
            var (result, error) = await paymentService.GetForEventAsync(id, userId.Value, IsModeratorOrAdmin(principal), locale, ct);
            if (error is not null)
            {
                var status = error == "Событие не найдено." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Ok(result);
        })
        .WithName("GetEventPayments")
        .WithTags("Payments")
        .RequireAuthorization()
        .Produces<List<PaymentDto>>()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPatch("/api/payments/{id:guid}", async (
            Guid id, UpdatePaymentStatusRequest request, ClaimsPrincipal principal, PaymentService paymentService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await paymentService.SetStatusAsync(id, userId.Value, IsModeratorOrAdmin(principal), request, ct);
            if (!ok)
            {
                var status = error == "Платёж не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("UpdatePaymentStatus")
        .WithTags("Payments")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/me/payments", async (HttpContext ctx, ClaimsPrincipal principal, PaymentService paymentService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.ResolveAndVary(ctx);
            return Results.Ok(await paymentService.GetMyPaymentsAsync(userId.Value, locale, ct));
        })
        .WithName("GetMyPayments")
        .WithTags("Payments")
        .RequireAuthorization()
        .Produces<List<MyPaymentDto>>();

        return app;
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

    private static bool IsModeratorOrAdmin(ClaimsPrincipal principal) =>
        GetRole(principal) is UserRole.Moderator or UserRole.Admin;
}
