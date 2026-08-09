using System.Security.Cryptography;
using System.Text;
using GameOrg.Api.Common;

namespace GameOrg.Api.Features.Identity;

/// <summary>
/// Нормализует источник (username/first_name от провайдера) в формат хендла
/// БД: `^[a-z][a-z0-9_]{2,29}$` (см. 001_constraints.sql, users_handle_format).
/// Az/ru буквы транслитерируются в латиницу (см. <see cref="Transliterator"/>)
/// вместо того, чтобы просто выбрасываться.
/// </summary>
public static class HandleGenerator
{
    public static string Normalize(string source)
    {
        var sb = new StringBuilder();
        foreach (var ch in Transliterator.Transliterate(source).ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
                sb.Append(ch);
            if (sb.Length == 30) break;
        }

        var candidate = sb.ToString();
        if (candidate.Length == 0 || !char.IsAsciiLetterLower(candidate[0]))
            candidate = "u" + candidate;

        candidate = candidate.Length > 30 ? candidate[..30] : candidate;

        // Минимальная длина по constraint'у — 3 символа.
        while (candidate.Length < 3)
            candidate += RandomSuffix(1);

        return candidate;
    }

    public static string WithSuffix(string baseHandle)
    {
        var suffix = RandomSuffix(4);
        var maxBaseLength = 30 - suffix.Length - 1;
        var trimmed = baseHandle.Length > maxBaseLength ? baseHandle[..maxBaseLength] : baseHandle;
        return $"{trimmed}_{suffix}";
    }

    private static string RandomSuffix(int length) =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes((length + 1) / 2))[..length];
}
