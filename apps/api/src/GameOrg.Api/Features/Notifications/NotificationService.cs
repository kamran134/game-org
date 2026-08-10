using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Notifications;

/// <summary>Читает только InApp-канал — Telegram-записи существуют для доставки, не для показа в списке.</summary>
public sealed class NotificationService(GameOrgDbContext db)
{
    public async Task<List<NotificationDto>> GetListAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct)
    {
        var query = db.Notifications.Where(n => n.UserId == userId && n.Channel == NotificationChannel.InApp);
        if (unreadOnly) query = query.Where(n => n.ReadAt == null);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(ct);

        return notifications.Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.Data, n.CreatedAt, n.ReadAt)).ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct) =>
        db.Notifications.CountAsync(n => n.UserId == userId && n.Channel == NotificationChannel.InApp && n.ReadAt == null, ct);

    public async Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && n.Channel == NotificationChannel.InApp, ct);
        if (notification is null) return false;

        notification.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.Notifications
            .Where(n => n.UserId == userId && n.Channel == NotificationChannel.InApp && n.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.ReadAt, now), ct);
    }

    /// <summary>
    /// Только реально вызываемые типы — остальные (ClubInvite, NewFollower, MvpVoteOpen...) числятся
    /// в enum'е под ещё не построенные фичи (Clubs/Follow/MvpVote/Payment), показывать для них
    /// переключатель было бы мёртвым UI. Список расширять по мере того, как эти типы начинают
    /// реально отправляться в NotificationSender.SendAsync.
    /// </summary>
    private static readonly NotificationType[] WiredTypes =
    [
        NotificationType.EventReminder24h,
        NotificationType.EventReminder2h,
        NotificationType.EventUpdated,
        NotificationType.EventCancelled,
        NotificationType.EventConfirmed,
        NotificationType.ParticipantJoined,
        NotificationType.ParticipantLeft,
        NotificationType.WaitlistPromoted,
    ];

    /// <summary>Opt-out: строки нет → включено. Один и тот же принцип, что в NotificationSender.IsChannelEnabledAsync.</summary>
    public async Task<List<NotificationPreferenceDto>> GetPreferencesAsync(Guid userId, CancellationToken ct)
    {
        var disabled = await db.NotificationPreferences
            .Where(p => p.UserId == userId && p.Channel == NotificationChannel.Telegram && !p.Enabled)
            .Select(p => p.Type)
            .ToListAsync(ct);
        var disabledSet = disabled.ToHashSet();

        return WiredTypes
            .Select(type => new NotificationPreferenceDto(type, !disabledSet.Contains(type)))
            .ToList();
    }

    public async Task SetPreferenceAsync(Guid userId, NotificationType type, bool enabled, CancellationToken ct)
    {
        var pref = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type && p.Channel == NotificationChannel.Telegram, ct);

        if (pref is null)
        {
            db.NotificationPreferences.Add(new NotificationPreference
            {
                UserId = userId,
                Type = type,
                Channel = NotificationChannel.Telegram,
                Enabled = enabled,
            });
        }
        else
        {
            pref.Enabled = enabled;
        }

        await db.SaveChangesAsync(ct);
    }
}
