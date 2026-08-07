using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Identity;

/// <summary>
/// Провайдер-агностичный поиск/создание аккаунта по <see cref="ExternalIdentity"/>.
/// Не меняется, когда добавляются новые провайдеры (Google/Apple) — им нужен
/// только новый validator, который производит тот же ExternalIdentity.
/// </summary>
public sealed class IdentityService(GameOrgDbContext db)
{
    public async Task<User> SignInAsync(ExternalIdentity identity, CancellationToken ct)
    {
        var account = await db.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Provider == identity.Provider && a.ProviderUserId == identity.ProviderUserId, ct);

        if (account is not null)
        {
            account.LastUsedAt = DateTime.UtcNow;
            if (identity.AvatarUrl is not null)
                account.Meta = new Dictionary<string, object> { ["avatarUrl"] = identity.AvatarUrl };

            await db.SaveChangesAsync(ct);
            return account.User;
        }

        var handle = await GenerateUniqueHandleAsync(identity, ct);

        var user = new User
        {
            Handle = handle,
            DisplayName = identity.DisplayName,
        };

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
