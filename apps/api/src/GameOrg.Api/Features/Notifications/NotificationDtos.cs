using GameOrg.Domain;

namespace GameOrg.Api.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string? Title,
    string? Body,
    Dictionary<string, object>? Data,
    DateTime CreatedAt,
    DateTime? ReadAt);

/// <summary>Только Telegram-канал в этом шаге — InApp всегда включён, его нельзя выключить (см. docs/PLAN.md, Шаг 11).</summary>
public sealed record NotificationPreferenceDto(NotificationType Type, bool Enabled);

public sealed record UpdateNotificationPreferenceRequest(bool Enabled);
