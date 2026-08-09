using System.Security.Cryptography;

namespace GameOrg.Api.Features.Events;

/// <summary>
/// Случайный публичный id для /e/{PublicId}. В отличие от
/// VenueSlugGenerator (слаг из названия площадки) — читаемость не нужна,
/// только компактность и уникальность: 8 hex-символов, под
/// EventConfiguration.PublicId (HasMaxLength(12)).
/// </summary>
public static class EventPublicIdGenerator
{
    public static string Generate() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4));
}
