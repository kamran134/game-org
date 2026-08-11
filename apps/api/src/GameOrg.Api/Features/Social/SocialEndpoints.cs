using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Social;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/social/follow", async (FollowRequest request, ClaimsPrincipal principal, FollowService followService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await followService.FollowAsync(userId.Value, request.TargetType, request.TargetId, ct);
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
        })
        .WithName("Follow")
        .WithTags("Social")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest);

        app.MapDelete("/api/social/follow", async (
            FollowTargetType targetType, Guid targetId, ClaimsPrincipal principal, FollowService followService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var removed = await followService.UnfollowAsync(userId.Value, targetType, targetId, ct);
            return removed ? Results.NoContent() : Results.NotFound();
        })
        .WithName("Unfollow")
        .WithTags("Social")
        .RequireAuthorization()
        .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/social/followers", async (
            FollowTargetType targetType, Guid targetId, int? skip, int? take, HttpContext ctx, FollowService followService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var result = await followService.GetFollowersAsync(targetType, targetId, locale, skip ?? 0, take ?? 30, ct);
            return Results.Ok(result);
        })
        .WithName("GetFollowers")
        .WithTags("Social")
        .Produces<List<FollowSummaryDto>>();

        app.MapGet("/api/social/following/{userId:guid}", async (
            Guid userId, FollowTargetType? targetType, int? skip, int? take, HttpContext ctx, FollowService followService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var result = await followService.GetFollowingAsync(userId, targetType, locale, skip ?? 0, take ?? 30, ct);
            return Results.Ok(result);
        })
        .WithName("GetFollowing")
        .WithTags("Social")
        .Produces<List<FollowSummaryDto>>();

        app.MapGet("/api/social/feed", async (
            int? skip, int? take, ClaimsPrincipal principal, HttpContext ctx, ActivityService activityService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.ResolveAndVary(ctx);
            var result = await activityService.GetFeedAsync(userId.Value, locale, skip ?? 0, take ?? 20, ct);
            return Results.Ok(result);
        })
        .WithName("GetFeed")
        .WithTags("Social")
        .RequireAuthorization()
        .Produces<List<ActivityDto>>();

        return app;
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var userId) ? userId : null;
    }
}
