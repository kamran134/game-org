using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Identity;

/// <summary>
/// Провайдер-агностичный поиск/создание аккаунта по <see cref="ExternalIdentity"/>.
/// Не меняется, когда добавляются новые провайдеры (Google/Apple) — им нужен
/// только новый validator, который производит тот же ExternalIdentity.
/// </summary>
public sealed class IdentityService(GameOrgDbContext db, IConfiguration configuration)
{
    public async Task<User?> SignInAsync(ExternalIdentity identity, string locale, CancellationToken ct)
    {
        var account = await db.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Provider == identity.Provider && a.ProviderUserId == identity.ProviderUserId, ct);

        if (account is not null)
        {
            // Забаненный не логинится — ни разу не проверялось раньше, из-за
            // чего Suspended ничего не блокировал (docs/PLAN.md, Шаг 9).
            if (account.User.Status != UserStatus.Active) return null;

            account.LastUsedAt = DateTime.UtcNow;
            if (identity.AvatarUrl is not null)
                account.Meta = new Dictionary<string, object> { ["avatarUrl"] = identity.AvatarUrl };

            PromoteToAdminIfListed(account.User, identity);
            await db.SaveChangesAsync(ct);
            return account.User;
        }

        var handle = await GenerateUniqueHandleAsync(identity, ct);

        var user = new User
        {
            Handle = handle,
            // Пишем имя от провайдера в язык, с которого регистрировались —
            // остальные языки человек дозаполнит сам в /me (см. docs/PLAN.md, Шаг 7.5).
            DisplayNameI18n = new Dictionary<string, string> { [locale] = identity.DisplayName },
        };
        PromoteToAdminIfListed(user, identity);

        var newAccount = new Account
        {
            UserId = user.Id,
            User = user,
            Provider = identity.Provider,
            ProviderUserId = identity.ProviderUserId,
            Email = identity.Email,
            Meta = identity.AvatarUrl is null ? null : new Dictionary<string, object> { ["avatarUrl"] = identity.AvatarUrl },
            LastUsedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.Accounts.Add(newAccount);
        await db.SaveChangesAsync(ct);

        return user;
    }

    /// <summary>
    /// ADMIN_TELEGRAM_IDS — список telegram_id через запятую в конфиге.
    /// Проверяем и при регистрации, и при каждом логине — иначе добавление
    /// id в список не подействовало бы на уже существующий аккаунт. Только
    /// повышает, никогда не понижает — иначе роль, выданную вручную,
    /// затёрлась бы при следующем входе, если id уберут из списка.
    /// </summary>
    private void PromoteToAdminIfListed(User user, ExternalIdentity identity)
    {
        if (identity.Provider != AuthProvider.Telegram || user.Role == UserRole.Admin) return;

        var adminIds = (configuration["ADMIN_TELEGRAM_IDS"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (adminIds.Contains(identity.ProviderUserId))
            user.Role = UserRole.Admin;
    }

    private async Task<string> GenerateUniqueHandleAsync(ExternalIdentity identity, CancellationToken ct)
    {
        var seed = identity.DisplayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? identity.DisplayName;
        var baseCandidate = HandleGenerator.Normalize(seed);

        var candidate = baseCandidate;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var taken = await db.Users.AnyAsync(u => u.Handle == candidate && u.DeletedAt == null, ct);
            if (!taken) return candidate;
            candidate = HandleGenerator.WithSuffix(baseCandidate);
        }

        throw new InvalidOperationException("Не удалось сгенерировать уникальный handle после 5 попыток.");
    }
}
