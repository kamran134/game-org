using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Notifications;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", async (
            ClaimsPrincipal principal, bool? unreadOnly, int? skip, int? take, NotificationService notificationService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var result = await notificationService.GetListAsync(userId.Value, unreadOnly ?? false, skip ?? 0, take ?? 20, ct);
            return Results.Ok(result);
        })
        .WithName("GetNotifications")
        .WithTags("Notifications")
        .RequireAuthorization()
        .Produces<List<NotificationDto>>();

        app.MapGet("/api/notifications/unread-count", async (ClaimsPrincipal principal, NotificationService notificationService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            return Results.Ok(new { count = await notificationService.GetUnreadCountAsync(userId.Value, ct) });
        })
        .WithName("GetUnreadNotificationCount")
        .WithTags("Notifications")
        .RequireAuthorization();

        app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, ClaimsPrincipal principal, NotificationService notificationService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var ok = await notificationService.MarkReadAsync(userId.Value, id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithName("MarkNotificationRead")
        .WithTags("Notifications")
        .RequireAuthorization()
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/notifications/read-all", async (ClaimsPrincipal principal, NotificationService notificationService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            await notificationService.MarkAllReadAsync(userId.Value, ct);
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsRead")
        .WithTags("Notifications")
        .RequireAuthorization();

        app.MapGet("/api/me/notification-preferences", async (ClaimsPrincipal principal, NotificationService notificationService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            return Results.Ok(await notificationService.GetPreferencesAsync(userId.Value, ct));
        })
        .WithName("GetNotificationPreferences")
        .WithTags("Notifications")
        .RequireAuthorization()
        .Produces<List<NotificationPreferenceDto>>();

        app.MapPut("/api/me/notification-preferences/{type}", async (
            NotificationType type, UpdateNotificationPreferenceRequest request, ClaimsPrincipal principal, NotificationService notificationService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            await notificationService.SetPreferenceAsync(userId.Value, type, request.Enabled, ct);
            return Results.NoContent();
        })
        .WithName("UpdateNotificationPreference")
        .WithTags("Notifications")
        .RequireAuthorization();

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
