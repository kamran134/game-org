namespace GameOrg.Domain.Entities;

/// <summary>Надёжность — глобальная, не по видам спорта. Ключевая метрика доверия.</summary>
public sealed class ReliabilityStat
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int Signups { get; set; }
    public int Attended { get; set; }
    public int NoShows { get; set; }
    public int LateCancels { get; set; }
    public int HostedEvents { get; set; }
    /// <summary>0..100, пересчитывается воркером с затуханием старых нарушений.</summary>
    public int Score { get; set; } = 100;
    /// <summary>Недель подряд с игрой.</summary>
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
