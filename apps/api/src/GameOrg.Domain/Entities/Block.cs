namespace GameOrg.Domain.Entities;

public sealed class Block
{
    public Guid BlockerId { get; set; }
    public User Blocker { get; set; } = null!;
    public Guid BlockedId { get; set; }
    public User Blocked { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
