namespace GameOrg.Domain.Entities;

/// <summary>Способ входа. TELEGRAM.ProviderUserId = telegram_id — мост к боту.</summary>
public sealed class Account
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public AuthProvider Provider { get; set; }
    public required string ProviderUserId { get; set; }
    public string? Email { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public string? PasswordHash { get; set; }
    public Dictionary<string, object>? Meta { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
