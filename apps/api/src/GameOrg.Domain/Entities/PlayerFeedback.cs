namespace GameOrg.Domain.Entities;

/// <summary>Оценка игрока игроком после матча. Анонимна на выдаче, агрегируется.</summary>
public sealed class PlayerFeedback
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public Guid TargetId { get; set; }
    public User Target { get; set; } = null!;
    /// <summary>1..5.</summary>
    public int? Skill { get; set; }
    /// <summary>1..5.</summary>
    public int? FairPlay { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
