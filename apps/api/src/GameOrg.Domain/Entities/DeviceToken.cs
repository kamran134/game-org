namespace GameOrg.Domain.Entities;

public sealed class DeviceToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DevicePlatform Platform { get; set; }
    public required string Token { get; set; }
    public string? Locale { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
