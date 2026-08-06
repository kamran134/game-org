namespace GameOrg.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }
    /// <summary>event.cancel, club.member.ban...</summary>
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public Dictionary<string, object>? Before { get; set; }
    public Dictionary<string, object>? After { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
