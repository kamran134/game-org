namespace GameOrg.Domain.Entities;

public sealed class UserSportPosition
{
    public Guid UserSportId { get; set; }
    public UserSport UserSport { get; set; } = null!;
    public Guid PositionId { get; set; }
    public SportPosition Position { get; set; } = null!;
    public bool IsPrimary { get; set; }
}
