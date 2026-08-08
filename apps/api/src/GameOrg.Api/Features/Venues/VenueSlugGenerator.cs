using System.Security.Cryptography;
using System.Text;

namespace GameOrg.Api.Features.Venues;

/// <summary>
/// Слаг из названия площадки. В отличие от HandleGenerator (Identity) — не
/// транслитерирует не-латиницу (названия часто на русском/азербайджанском):
/// просто берёт латиницу/цифры из имени и ВСЕГДА добавляет случайный суффикс,
/// чтобы не зависеть от читаемости для не-латинских названий и не бороться за
/// уникальность голого имени.
/// </summary>
public static class VenueSlugGenerator
{
    public static string Generate(string name)
    {
        var sb = new StringBuilder();
        var lastWasHyphen = true; // чтобы слаг не начинался с дефиса

        foreach (var ch in name.ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && sb.Length > 0)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }

            if (sb.Length >= 60) break;
        }

        var slug = sb.ToString().Trim('-');
        var suffix = RandomSuffix(4);
        return slug.Length == 0 ? $"venue-{suffix}" : $"{slug}-{suffix}";
    }

    private static string RandomSuffix(int length) =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes((length + 1) / 2))[..length];
}
