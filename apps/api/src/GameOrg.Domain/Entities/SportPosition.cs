namespace GameOrg.Domain.Entities;

public sealed class SportPosition
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    /// <summary>GK, CB, LW, SETTER, LIBERO...</summary>
    public required string Code { get; set; }
    public required Dictionary<string, string> NameI18n { get; set; }
    public int SortOrder { get; set; }
}
