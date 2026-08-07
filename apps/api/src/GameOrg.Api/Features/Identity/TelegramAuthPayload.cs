using System.Text.Json.Serialization;

namespace GameOrg.Api.Features.Identity;

/// <summary>Сырые поля, которые присылает Telegram Login Widget колбэком.</summary>
public sealed record TelegramAuthPayload(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl,
    [property: JsonPropertyName("auth_date")] long AuthDate,
    [property: JsonPropertyName("hash")] string Hash);
