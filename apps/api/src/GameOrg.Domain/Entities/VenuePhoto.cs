namespace GameOrg.Domain.Entities;

public sealed class VenuePhoto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public Guid MediaId { get; set; }
    public MediaAsset Media { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
}
