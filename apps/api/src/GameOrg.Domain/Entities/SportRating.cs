namespace GameOrg.Domain.Entities;

/// <summary>Glicko-2 на каждый вид спорта отдельно.</summary>
public sealed class SportRating
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    public double Rating { get; set; } = 1500;
    public double Deviation { get; set; } = 350;
    public double Volatility { get; set; } = 0.06;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int MvpCount { get; set; }
    public double PeakRating { get; set; } = 1500;
    public DateTime? LastPlayedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
