namespace GameOrg.Domain.Entities;

public sealed class VenueSport
{
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    public int Courts { get; set; } = 1;
}
