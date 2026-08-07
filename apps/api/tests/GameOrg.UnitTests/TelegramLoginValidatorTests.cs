using System.Security.Cryptography;
using System.Text;
using GameOrg.Api.Features.Identity;

namespace GameOrg.UnitTests;

public class TelegramLoginValidatorTests
{
    private const string BotToken = "test:token";

    [Fact]
    public void TryValidate_accepts_correctly_signed_payload()
    {
        var payload = SignedPayload(authDate: DateTimeOffset.UtcNow);

        var ok = TelegramLoginValidator.TryValidate(payload, BotToken, out var identity);

        Assert.True(ok);
        Assert.NotNull(identity);
        Assert.Equal("123456", identity!.ProviderUserId);
        Assert.Equal("Rustam Kazimov", identity.DisplayName);
    }

    [Fact]
    public void TryValidate_rejects_tampered_hash()
    {
        var payload = SignedPayload(authDate: DateTimeOffset.UtcNow) with { Hash = "deadbeef" };

        var ok = TelegramLoginValidator.TryValidate(payload, BotToken, out var identity);

        Assert.False(ok);
        Assert.Null(identity);
    }

    [Fact]
    public void TryValidate_rejects_stale_auth_date()
    {
        var payload = SignedPayload(authDate: DateTimeOffset.UtcNow.AddHours(-25));

        var ok = TelegramLoginValidator.TryValidate(payload, BotToken, out var identity);

        Assert.False(ok);
        Assert.Null(identity);
    }

    private static TelegramAuthPayload SignedPayload(DateTimeOffset authDate)
    {
        var authDateUnix = authDate.ToUnixTimeSeconds();

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDateUnix.ToString(),
            ["first_name"] = "Rustam",
            ["id"] = "123456",
            ["last_name"] = "Kazimov",
            ["username"] = "rustam",
        };
        var dataCheckString = string.Join('\n', fields.Select(kv => $"{kv.Key}={kv.Value}"));

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(BotToken));
        var hash = Convert.ToHexStringLower(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)));

        return new TelegramAuthPayload(
            Id: 123456,
            FirstName: "Rustam",
            LastName: "Kazimov",
            Username: "rustam",
            PhotoUrl: null,
            AuthDate: authDateUnix,
            Hash: hash);
    }
}
