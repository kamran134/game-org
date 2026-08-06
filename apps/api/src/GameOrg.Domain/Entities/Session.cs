namespace GameOrg.Domain.Entities;

public sealed class Session
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    /// <summary>sha256 от refresh-токена — сам токен не хранится.</summary>
    public required string RefreshTokenHash { get; set; }
    public string? UserAgent { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
