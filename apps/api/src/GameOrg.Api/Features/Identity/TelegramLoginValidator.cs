using System.Security.Cryptography;
using System.Text;
using GameOrg.Domain;

namespace GameOrg.Api.Features.Identity;

/// <summary>
/// Проверка подписи Telegram Login Widget: https://core.telegram.org/widgets/login#checking-authorization.
/// Единственное место, которое знает про формат Telegram-подписи — на выходе
/// провайдер-агностичный <see cref="ExternalIdentity"/>.
/// </summary>
public static class TelegramLoginValidator
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    public static bool TryValidate(
        TelegramAuthPayload payload,
        string botToken,
        out ExternalIdentity? identity)
    {
        identity = null;

        var authDate = DateTimeOffset.FromUnixTimeSeconds(payload.AuthDate);
        if (DateTimeOffset.UtcNow - authDate > MaxAge)
            return false;

        var dataCheckString = BuildDataCheckString(payload);
        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
        var computedHash = Convert.ToHexStringLower(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)));

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(payload.Hash.ToLowerInvariant())))
        {
            return false;
        }

        var displayName = string.IsNullOrWhiteSpace(payload.LastName)
            ? payload.FirstName
            : $"{payload.FirstName} {payload.LastName}";

        identity = new ExternalIdentity(
            AuthProvider.Telegram,
            payload.Id.ToString(),
            Email: null,
            DisplayName: displayName,
            AvatarUrl: payload.PhotoUrl);

        return true;
    }

    private static string BuildDataCheckString(TelegramAuthPayload payload)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = payload.AuthDate.ToString(),
            ["first_name"] = payload.FirstName,
            ["id"] = payload.Id.ToString(),
        };

        if (!string.IsNullOrEmpty(payload.LastName)) fields["last_name"] = payload.LastName;
        if (!string.IsNullOrEmpty(payload.PhotoUrl)) fields["photo_url"] = payload.PhotoUrl;
        if (!string.IsNullOrEmpty(payload.Username)) fields["username"] = payload.Username;

        return string.Join('\n', fields.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
