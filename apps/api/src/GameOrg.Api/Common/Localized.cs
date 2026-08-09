namespace GameOrg.Api.Common;

/// <summary>
/// Фолбэк для мультиязычных полей (Venue.NameI18n, User.DisplayNameI18n, ...):
/// запрошенный язык → az → en → ru → первый непустой. Решение пользователя —
/// никогда не показывать пустоту вместо текста, и не помечать, что это
/// текст на другом языке (см. docs/PLAN.md, Шаг 7.5).
/// </summary>
public static class Localized
{
    // az первым — дефолт сайта; дальше en (не ru) — так решил пользователь.
    private static readonly string[] FallbackOrder = ["az", "en", "ru"];

    /// <summary>Пустые строки считаются отсутствующими — форма могла сохранить "" вместо null.</summary>
    public static string? Resolve(IReadOnlyDictionary<string, string>? values, string locale)
    {
        if (values is null) return null;

        if (TryGetNonEmpty(values, locale, out var direct)) return direct;

        foreach (var fallback in FallbackOrder)
            if (TryGetNonEmpty(values, fallback, out var value))
                return value;

        foreach (var value in values.Values)
            if (!string.IsNullOrEmpty(value))
                return value;

        return null;
    }

    private static bool TryGetNonEmpty(IReadOnlyDictionary<string, string> values, string key, out string value)
    {
        value = values.TryGetValue(key, out var v) ? v : "";
        return !string.IsNullOrEmpty(value);
    }
}
