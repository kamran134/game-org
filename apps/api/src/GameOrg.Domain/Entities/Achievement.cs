namespace GameOrg.Domain.Entities;

public sealed class Achievement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>FIRST_GAME, TEN_GAMES, IRON_MAN...</summary>
    public required string Code { get; set; }
    public required Dictionary<string, string> NameI18n { get; set; }
    public required Dictionary<string, string> DescI18n { get; set; }
    public string? Icon { get; set; }
    public int Tier { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<UserAchievement> Users { get; set; } = new List<UserAchievement>();
}
