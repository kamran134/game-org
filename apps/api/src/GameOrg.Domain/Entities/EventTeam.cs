namespace GameOrg.Domain.Entities;

public sealed class EventTeam
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public required string Name { get; set; }
    public string? ColorHex { get; set; }
    public int? Score { get; set; }
    public int SortOrder { get; set; }

    public ICollection<EventParticipant> Members { get; set; } = new List<EventParticipant>();
}
