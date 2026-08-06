namespace GameOrg.Domain.Entities;

/// <summary>
/// Лента. Фаза 2 — fan-out on read (выборка по подпискам).
/// Когда упрёмся в производительность — добавляем feed_entries с fan-out on write.
/// </summary>
public sealed class Activity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public ActivityVerb Verb { get; set; }

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }
    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    public Visibility Audience { get; set; } = Visibility.Public;
    public Dictionary<string, object>? Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
