namespace GameOrg.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Queued;

    public string? Title { get; set; }
    public string? Body { get; set; }
    /// <summary>{ eventId, clubId, deepLink }.</summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Дедупликация: один и тот же повод не шлём дважды.
    /// Напр. "EVENT_REMINDER_24H:{eventId}:{userId}".
    /// </summary>
    public string? DedupeKey { get; set; }

    public DateTime? ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Error { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
