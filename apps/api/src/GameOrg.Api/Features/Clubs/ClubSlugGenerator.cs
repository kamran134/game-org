using System.Security.Cryptography;
using System.Text;
using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Clubs;

/// <summary>Копия VenueSlugGenerator — по конвенции проекта мелкий дублирующийся хелпер лучше общей абстракции.</summary>
public static class ClubSlugGenerator
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
        return slug.Length == 0 ? $"club-{suffix}" : $"{slug}-{suffix}";
    }

    private static string RandomSuffix(int length) =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes((length + 1) / 2))[..length];
}
