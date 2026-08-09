using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GameOrg.Api.Features.Identity;

/// <summary>
/// Выпуск/обновление/отзыв JWT access + refresh токенов. Провайдер-агностично —
/// работает от <see cref="User"/>, ничего не знает про Telegram/Google/Apple.
/// </summary>
public sealed class TokenService(GameOrgDbContext db, IConfiguration configuration, IHostEnvironment env)
{
    public const string Issuer = "game-org-api";
    public const string Audience = "game-org-clients";
    private const string AccessCookieName = "go_access";
    private const string RefreshCookieName = "go_refresh";
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task IssueTokensAsync(User user, HttpContext ctx, CancellationToken ct)
    {
        var accessToken = CreateAccessToken(user);
        var refreshToken = await CreateSessionAsync(user, ctx, ct);

        ctx.Response.Cookies.Append(AccessCookieName, accessToken, CookieOptions(ttl: AccessTokenLifetime, path: "/"));
        ctx.Response.Cookies.Append(RefreshCookieName, refreshToken, CookieOptions(ttl: RefreshTokenLifetime, path: "/api/auth"));
    }

    /// <summary>Ротирует refresh-сессию и выдаёт новую пару токенов. Null — если cookie нет/сессия невалидна.</summary>
    public async Task<User?> RefreshAsync(HttpContext ctx, CancellationToken ct)
    {
        if (!ctx.Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken) || string.IsNullOrEmpty(rawToken))
            return null;

        var hash = HashToken(rawToken);
        var session = await db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow, ct);

        // null — забаненный (Status != Active): сессия ещё жива, но новых
        // токенов не получает, та же проверка, что при логине
        // (IdentityService.SignInAsync). Не отзываем сессию — просто не
        // продлеваем; если разбанят, следующий вызов снова заработает.
        if (session is null || session.User.Status != UserStatus.Active)
            return null;

        session.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await IssueTokensAsync(session.User, ctx, ct);
        return session.User;
    }

    public async Task RevokeAsync(HttpContext ctx, CancellationToken ct)
    {
        if (ctx.Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken) && !string.IsNullOrEmpty(rawToken))
        {
            var hash = HashToken(rawToken);
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash && s.RevokedAt == null, ct);
            if (session is not null)
            {
                session.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        ctx.Response.Cookies.Delete(AccessCookieName, CookieOptions(ttl: TimeSpan.Zero, path: "/"));
        ctx.Response.Cookies.Delete(RefreshCookieName, CookieOptions(ttl: TimeSpan.Zero, path: "/api/auth"));
    }

    private string CreateAccessToken(User user)
    {
        var signingKey = configuration["JWT_SIGNING_KEY"]
            ?? throw new InvalidOperationException("JWT_SIGNING_KEY не задан.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("handle", user.Handle),
            new Claim("role", user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateSessionAsync(User user, HttpContext ctx, CancellationToken ct)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        db.Sessions.Add(new Session
        {
            UserId = user.Id,
            RefreshTokenHash = HashToken(rawToken),
            UserAgent = ctx.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null,
            Ip = ctx.Connection.RemoteIpAddress?.ToString(),
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
        });

        await db.SaveChangesAsync(ct);
        return rawToken;
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private CookieOptions CookieOptions(TimeSpan ttl, string path) => new()
    {
        HttpOnly = true,
        Secure = !env.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = path,
        Expires = ttl == TimeSpan.Zero ? DateTimeOffset.UnixEpoch : DateTimeOffset.UtcNow.Add(ttl),
    };
}
