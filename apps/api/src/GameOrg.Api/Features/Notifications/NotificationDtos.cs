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
