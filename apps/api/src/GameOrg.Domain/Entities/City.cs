namespace GameOrg.Domain.Entities;

public sealed class City
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Slug { get; set; }
    public required Dictionary<string, string> NameI18n { get; set; }
    public required string CountryCode { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Timezone { get; set; } = "Asia/Baku";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
