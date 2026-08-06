namespace GameOrg.Domain.Entities;

public sealed class MvpVote
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid VoterId { get; set; }
    public User Voter { get; set; } = null!;
    public Guid TargetUserId { get; set; }
    public User TargetUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
