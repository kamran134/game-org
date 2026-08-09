namespace GameOrg.Api.Common;

/// <summary>
/// Мультиязычное текстовое поле в API — типизированная запись вместо
/// <c>Dictionary&lt;string,string&gt;</c>. Причина: Kiota на голый словарь
/// выдаёт <c>additionalData</c>-мешок вместо нормального типа (так сейчас
/// читаются NameI18n у городов на фронте) — с record такого не происходит.
/// Хранилище (домен/БД) при этом остаётся словарём: так можно добавить
/// четвёртый язык без миграции, конвертация — только на границе API.
/// </summary>
public sealed record LocalizedTextDto(string? Az, string? Ru, string? En)
{
    public bool IsEmpty => string.IsNullOrEmpty(Az) && string.IsNullOrEmpty(Ru) && string.IsNullOrEmpty(En);

    /// <summary>true, если хотя бы один заполненный язык длиннее maxLength.</summary>
    public bool ExceedsMaxLength(int maxLength) =>
        (Az?.Length ?? 0) > maxLength || (Ru?.Length ?? 0) > maxLength || (En?.Length ?? 0) > maxLength;

    /// <summary>Пустые/отсутствующие языки не попадают в словарь. Всё пусто → null (сохранённого текста нет).</summary>
    public Dictionary<string, string>? ToDict()
    {
        var dict = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(Az)) dict["az"] = Az;
        if (!string.IsNullOrEmpty(Ru)) dict["ru"] = Ru;
        if (!string.IsNullOrEmpty(En)) dict["en"] = En;
        return dict.Count == 0 ? null : dict;
    }

    public static LocalizedTextDto From(IReadOnlyDictionary<string, string> dict) =>
        new(dict.GetValueOrDefault("az"), dict.GetValueOrDefault("ru"), dict.GetValueOrDefault("en"));

    public static LocalizedTextDto? FromNullable(IReadOnlyDictionary<string, string>? dict) =>
        dict is null ? null : From(dict);
}
