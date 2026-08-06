namespace GameOrg.Domain.Entities;

public sealed class Sport
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>football, volleyball, basketball...</summary>
    public required string Slug { get; set; }
    public required Dictionary<string, string> NameI18n { get; set; }
    public string? Emoji { get; set; }
    /// <summary>5 для мини-футбола, 6 для волейбола.</summary>
    public int? DefaultTeamSize { get; set; }
    public bool HasPositions { get; set; } = true;
    public bool HasScore { get; set; } = true;
    public bool IsTeamSport { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<SportPosition> Positions { get; set; } = new List<SportPosition>();
}
