using System.Security.Cryptography;
using System.Text;
using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Venues;

/// <summary>
/// Слаг из названия площадки. Az/ru буквы транслитерируются в латиницу
/// (см. <see cref="Transliterator"/>), всё остальное не-латинское — просто
/// выбрасывается. ВСЕГДА добавляет случайный суффикс, чтобы не бороться за
/// уникальность голого имени (и не зависеть от того, осталось ли что-то
/// читаемое после транслитерации).
/// </summary>
public static class VenueSlugGenerator
{
    public static string Generate(string name)
    {
        var sb = new StringBuilder();
        var lastWasHyphen = true; // чтобы слаг не начинался с дефиса

        foreach (var ch in Transliterator.Transliterate(name).ToLowerInvariant())
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
