using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameOrg.Api.Common;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Clubs;

public static class ClubsEndpoints
{
    public static IEndpointRouteBuilder MapClubsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/clubs", async (
            HttpContext ctx, ClaimsPrincipal principal, Guid? cityId, Guid? sportId, ClubKind? kind, ClubService clubService, CancellationToken ct) =>
        {
            var locale = RequestLocale.ResolveAndVary(ctx);
            var clubs = await clubService.GetListAsync(locale, GetUserId(principal), cityId, sportId, kind, ct);
            return Results.Ok(clubs);
        })
        .WithName("GetClubs")
        .WithTags("Clubs")
        .Produces<List<ClubDto>>();

        app.MapGet("/api/clubs/mine", async (HttpContext ctx, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            return Results.Ok(await clubService.GetMyClubsAsync(userId.Value, locale, ct));
        })
        .WithName("GetMyClubs")
        .WithTags("Clubs")
        .RequireAuthorization()
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

        app.MapPost("/api/clubs/{id:guid}/avatar/presign", async (
            Guid id, PresignClubAvatarRequest request, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (result, error) = await clubService.PresignAvatarAsync(id, userId.Value, request, ct);
            if (error is not null)
            {
                var status = error.StartsWith("Загружать") ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Ok(result);
        })
        .WithName("PresignClubAvatar")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces<PresignClubAvatarResponse>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        app.MapPost("/api/clubs/{id:guid}/avatar", async (
            Guid id, AttachClubAvatarRequest request, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.SetAvatarAsync(id, userId.Value, request, ct);
            if (!ok)
            {
                var status = error == "Клуб не найден." || error == "Медиа не найдено."
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.NoContent();
        })
        .WithName("SetClubAvatar")
        .WithTags("Clubs")
        .RequireAuthorization()
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

        app.MapPost("/api/clubs/{id:guid}/join", async (Guid id, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.JoinAsync(id, userId.Value, ct);
            var status = error == "Клуб не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("JoinClub")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clubs/{id:guid}/leave", async (Guid id, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.LeaveAsync(id, userId.Value, ct);
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
        })
        .WithName("LeaveClub")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest);

        app.MapPost("/api/clubs/{id:guid}/invite", async (Guid id, InviteMemberRequest request, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.InviteAsync(id, userId.Value, request.UserId, ct);
            var status = error == "Приглашать может только владелец или админ клуба." ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("InviteClubMember")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden);

        app.MapGet("/api/clubs/{id:guid}/members", async (Guid id, HttpContext ctx, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var (members, error) = await clubService.GetMembersAsync(id, userId.Value, locale, ct);
            return members is not null ? Results.Ok(members) : Results.Problem(error, statusCode: StatusCodes.Status403Forbidden);
        })
        .WithName("GetClubMembers")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces<List<ClubMemberDto>>()
        .Produces(StatusCodes.Status403Forbidden);

        app.MapGet("/api/clubs/{id:guid}/join-requests", async (Guid id, HttpContext ctx, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var (requests, error) = await clubService.GetJoinRequestsAsync(id, userId.Value, locale, ct);
            return requests is not null ? Results.Ok(requests) : Results.Problem(error, statusCode: StatusCodes.Status403Forbidden);
        })
        .WithName("GetClubJoinRequests")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces<List<ClubMemberDto>>()
        .Produces(StatusCodes.Status403Forbidden);

        app.MapPost("/api/clubs/{id:guid}/join-requests/{userId:guid}/approve", async (Guid id, Guid userId, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (actorId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.ApproveJoinRequestAsync(id, actorId.Value, userId, ct);
            var status = error == "Заявка не найдена." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("ApproveClubJoinRequest")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clubs/{id:guid}/join-requests/{userId:guid}/reject", async (Guid id, Guid userId, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (actorId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.RejectJoinRequestAsync(id, actorId.Value, userId, ct);
            var status = error == "Заявка не найдена." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("RejectClubJoinRequest")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/clubs/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (actorId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.RemoveMemberAsync(id, actorId.Value, userId, ct);
            var status = error == "Участник не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("RemoveClubMember")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPatch("/api/clubs/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, SetMemberRoleRequest request, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (actorId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.SetRoleAsync(id, actorId.Value, userId, request.Role, ct);
            var status = error == "Участник не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("SetClubMemberRole")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clubs/{id:guid}/members/{userId:guid}/transfer-ownership", async (Guid id, Guid userId, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var actorId = GetUserId(principal);
            if (actorId is null) return Results.Unauthorized();

            var (ok, error) = await clubService.TransferOwnershipAsync(id, actorId.Value, userId, ct);
            var status = error == "Участник не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
            return ok ? Results.NoContent() : Results.Problem(error, statusCode: status);
        })
        .WithName("TransferClubOwnership")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clubs/join/{inviteCode}", async (string inviteCode, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (club, error) = await clubService.JoinByInviteCodeAsync(inviteCode, userId.Value, ct);
            if (error is not null)
            {
                var status = error == "Код приглашения не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Ok(new { club!.Slug });
        })
        .WithName("JoinClubByInviteCode")
        .WithTags("Clubs")
        .RequireAuthorization()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clubs/{id:guid}/invite-code/regenerate", async (Guid id, ClaimsPrincipal principal, ClubService clubService, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            if (userId is null) return Results.Unauthorized();

            var (code, error) = await clubService.RegenerateInviteCodeAsync(id, userId.Value, ct);
            if (error is not null)
            {
                var status = error == "Клуб не найден." ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                return Results.Problem(error, statusCode: status);
            }

            return Results.Ok(new { code });
        })
        .WithName("RegenerateClubInviteCode")
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
