using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Identity;

public sealed record AuthResponseDto(Guid UserId, string Handle, string DisplayName);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/telegram", async (
            TelegramAuthPayload payload,
            IdentityService identityService,
            TokenService tokenService,
            IConfiguration configuration,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var botToken = configuration["TELEGRAM_BOT_TOKEN"];
            if (string.IsNullOrEmpty(botToken))
                return Results.Problem("TELEGRAM_BOT_TOKEN не настроен на сервере.", statusCode: StatusCodes.Status500InternalServerError);

            if (!TelegramLoginValidator.TryValidate(payload, botToken, out var identity) || identity is null)
                return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            var user = await identityService.SignInAsync(identity, locale, ct);
            await tokenService.IssueTokensAsync(user, ctx, ct);

            return Results.Ok(new AuthResponseDto(user.Id, user.Handle, Localized.Resolve(user.DisplayNameI18n, locale) ?? ""));
        })
        .WithName("AuthTelegram")
        .WithTags("Auth")
        .Produces<AuthResponseDto>()
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/auth/refresh", async (TokenService tokenService, HttpContext ctx, CancellationToken ct) =>
        {
            var user = await tokenService.RefreshAsync(ctx, ct);
            if (user is null) return Results.Unauthorized();

            var locale = RequestLocale.Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
            return Results.Ok(new AuthResponseDto(user.Id, user.Handle, Localized.Resolve(user.DisplayNameI18n, locale) ?? ""));
        })
        .WithName("AuthRefresh")
        .WithTags("Auth")
        .Produces<AuthResponseDto>()
        .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/auth/logout", async (TokenService tokenService, HttpContext ctx, CancellationToken ct) =>
        {
            await tokenService.RevokeAsync(ctx, ct);
            return Results.NoContent();
        })
        .WithName("AuthLogout")
        .WithTags("Auth");

        return app;
    }
}
