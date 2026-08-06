namespace GameOrg.Domain.Entities;

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? OwnerId { get; set; }
    public User? Owner { get; set; }
    public MediaKind Kind { get; set; } = MediaKind.Image;
    /// <summary>Ключ в R2/S3.</summary>
    public required string BucketKey { get; set; }
    public required string MimeType { get; set; }
    public int SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Blurhash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
