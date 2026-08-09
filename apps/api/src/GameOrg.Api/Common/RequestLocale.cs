namespace GameOrg.Api.Common;

/// <summary>
/// Три локали сайта (apps/web/messages/*.json) — единственный источник
/// правды. На него ссылаются и валидация User.Locale (ProfileService), и
/// резолвинг Accept-Language для мультиязычного контента (см. <see cref="Localized"/>).
/// </summary>
public static class RequestLocale
{
    public const string Default = "az";
    public static readonly IReadOnlyList<string> Supported = ["az", "ru", "en"];

    /// <summary>
    /// Первый поддерживаемый язык из Accept-Language, иначе <see cref="Default"/>.
    /// q-веса не разбираются — при трёх вариантах порядок перечисления
    /// клиентом (то, что реально шлёт fetch с явным заголовком) достаточен.
    /// </summary>
    public static string Resolve(string? acceptLanguageHeader)
    {
        if (string.IsNullOrEmpty(acceptLanguageHeader)) return Default;

        foreach (var raw in acceptLanguageHeader.Split(','))
        {
            var primary = raw.Split(';')[0].Trim().Split('-')[0].ToLowerInvariant();
            if (primary.Length == 0) continue;
            if (Supported.Contains(primary)) return primary;
        }

        return Default;
    }

    /// <summary>
    /// Резолвит локаль из запроса и сразу ставит Vary: Accept-Language на
    /// ответ — тело меняется в зависимости от этого заголовка, значит любой
    /// кэш (CDN, браузер) должен об этом знать. Вызывать в каждом read-эндпоинте,
    /// который отдаёт локализованный текст.
    /// </summary>
    public static string ResolveAndVary(HttpContext ctx)
    {
        ctx.Response.Headers.Append("Vary", "Accept-Language");
        return Resolve(ctx.Request.Headers.AcceptLanguage.ToString());
    }
}
