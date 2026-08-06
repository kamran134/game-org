namespace GameOrg.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool Enabled { get; set; } = true;
}
