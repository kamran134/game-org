namespace GameOrg.Domain.Entities;

public sealed class ClubSport
{
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
}
