namespace GameOrg.Domain.Entities;

/// <summary>
/// Шаблон повторяющихся игр («каждый вторник 20:00»).
/// Воркер материализует Event'ы на горизонт вперёд (напр. 8 недель).
/// </summary>
public sealed class EventSeries
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }
    /// <summary>RFC 5545: FREQ=WEEKLY;BYDAY=TU.</summary>
    public required string Rrule { get; set; }
    public string Timezone { get; set; } = "Asia/Baku";
    /// <summary>"20:00" локального времени.</summary>
    public required string StartTime { get; set; }
    public int DurationMin { get; set; } = 90;
    /// <summary>Поля Event: title, cost, maxParticipants...</summary>
    public required Dictionary<string, object> Template { get; set; }
    public DateTime? MaterializedUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
