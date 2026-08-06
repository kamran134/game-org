namespace GameOrg.Domain.Entities;

public sealed class UserAchievement
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    /// <summary>{ eventId } — за что выдано.</summary>
    public Dictionary<string, object>? Context { get; set; }
}
