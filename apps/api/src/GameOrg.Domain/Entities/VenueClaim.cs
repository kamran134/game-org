namespace GameOrg.Domain.Entities;

/// <summary>Заявка владельца площадки на управление карточкой.</summary>
public sealed class VenueClaim
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public string? Evidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
