namespace GameOrg.Domain.Entities;

/// <summary>
/// Участник. UserId = null → гость, приведённый другим игроком.
/// Postgres считает NULL'ы различными в unique-индексе (EventId, UserId),
/// поэтому несколько гостей на одном событии не конфликтуют.
/// </summary>
public sealed class EventParticipant
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string? GuestName { get; set; }
    public Guid? InvitedById { get; set; }
    public User? InvitedBy { get; set; }

    public ParticipationStatus Status { get; set; } = ParticipationStatus.Confirmed;
    public Guid? PositionId { get; set; }
    public SportPosition? Position { get; set; }

    /// <summary>Порядок в очереди ожидания. Null для не-Waitlisted.</summary>
    public int? WaitlistOrder { get; set; }

    public Guid? TeamId { get; set; }
    public EventTeam? Team { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime StatusChangedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAt { get; set; }
    public string? Note { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
