namespace GameOrg.Domain.Entities;

public sealed class EventResult
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string? Summary { get; set; }
    /// <summary>Для не-командных видов: [{ userId, place, score }].</summary>
    public List<Dictionary<string, object>>? Standings { get; set; }
    public Guid? MvpUserId { get; set; }
    public Guid? RecordedById { get; set; }
    public User? RecordedBy { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    /// <summary>True — рейтинги уже пересчитаны воркером (идемпотентность).</summary>
    public bool RatingsApplied { get; set; }
}
