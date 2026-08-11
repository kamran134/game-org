using GameOrg.Domain;
using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Infrastructure.Notifications;

/// <summary>
/// Общая точка для "написать Notification и сразу попытаться отправить" —
/// используется и немедленной отправкой (EventService: отмена события,
/// повышение из вейтлиста), и плановой (EventReminderJob). На один повод
/// пишет до трёх записей — InApp (всегда, сразу Sent — читает её фронт),
/// Telegram и Push (Шаг 17) — оба только если NotificationPreference не
/// выключил канал явно. (DedupeKey, Channel) unique в БД — разные Channel
/// у одного DedupeKey не конфликтуют, схема именно под этот fan-out и
/// рассчитана (Шаг 11).
/// </summary>
public sealed class NotificationSender(GameOrgDbContext db, TelegramSender telegramSender, WebPushSender webPushSender)
{
    public async Task SendAsync(
        Guid userId, NotificationType type, string dedupeKey, string text, Dictionary<string, object>? data, CancellationToken ct)
    {
        await SendInAppAsync(userId, type, dedupeKey, text, data, ct);
        await SendTelegramAsync(userId, type, dedupeKey, text, data, ct);
        await SendPushAsync(userId, type, dedupeKey, text, data, ct);
    }

    private async Task SendInAppAsync(
        Guid userId, NotificationType type, string dedupeKey, string text, Dictionary<string, object>? data, CancellationToken ct)
    {
        var exists = await db.Notifications.AnyAsync(n => n.DedupeKey == dedupeKey && n.Channel == NotificationChannel.InApp, ct);
        if (exists) return;

        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Channel = NotificationChannel.InApp,
            Body = text,
            Data = data,
            DedupeKey = dedupeKey,
            Status = DeliveryStatus.Sent,
            SentAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task SendTelegramAsync(
        Guid userId, NotificationType type, string dedupeKey, string text, Dictionary<string, object>? data, CancellationToken ct)
    {
        var exists = await db.Notifications.AnyAsync(n => n.DedupeKey == dedupeKey && n.Channel == NotificationChannel.Telegram, ct);
        if (exists) return;
        if (!await IsChannelEnabledAsync(userId, type, NotificationChannel.Telegram, ct)) return;

        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Channel = NotificationChannel.Telegram,
            Body = text,
            Data = data,
            DedupeKey = dedupeKey,
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        var chatId = await db.Accounts
            .Where(a => a.UserId == userId && a.Provider == AuthProvider.Telegram)
            .Select(a => a.ProviderUserId)
            .FirstOrDefaultAsync(ct);

        if (chatId is null)
        {
            notification.Status = DeliveryStatus.Skipped;
            notification.Error = "У пользователя нет привязанного Telegram-аккаунта.";
            await db.SaveChangesAsync(ct);
            return;
        }

        notification.Attempts++;
        var sent = await telegramSender.TrySendAsync(chatId, text, ct);
        notification.Status = sent ? DeliveryStatus.Sent : DeliveryStatus.Failed;
        notification.SentAt = sent ? DateTime.UtcNow : null;
        notification.Error = sent ? null : "Не удалось отправить сообщение в Telegram.";
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// В отличие от Telegram (один чат) — шлёт на все Web-подписки пользователя.
    /// Запись Notification одна на (user, dedupeKey), статус — агрегат по всем устройствам:
    /// Sent, если хоть одно приняло, Failed — если все отвалились. Протухшие (404/410 от
    /// push-сервиса) DeviceToken удаляются сразу — самоочистка без отдельного воркера.
    /// </summary>
    private async Task SendPushAsync(
        Guid userId, NotificationType type, string dedupeKey, string text, Dictionary<string, object>? data, CancellationToken ct)
    {
        var exists = await db.Notifications.AnyAsync(n => n.DedupeKey == dedupeKey && n.Channel == NotificationChannel.Push, ct);
        if (exists) return;
        if (!await IsChannelEnabledAsync(userId, type, NotificationChannel.Push, ct)) return;

        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Channel = NotificationChannel.Push,
            Body = text,
            Data = data,
            DedupeKey = dedupeKey,
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        var devices = await db.DeviceTokens.Where(d => d.UserId == userId && d.Platform == DevicePlatform.Web).ToListAsync(ct);
        if (devices.Count == 0)
        {
            notification.Status = DeliveryStatus.Skipped;
            notification.Error = "У пользователя нет ни одной подписки на push-уведомления.";
            await db.SaveChangesAsync(ct);
            return;
        }

        notification.Attempts++;
        var anySent = false;
        var expired = new List<DeviceToken>();
        foreach (var device in devices)
        {
            var (sent, isExpired) = await webPushSender.TrySendAsync(device, "game.org.az", text, ct);
            if (sent) anySent = true;
            if (isExpired) expired.Add(device);
        }

        if (expired.Count > 0) db.DeviceTokens.RemoveRange(expired);

        notification.Status = anySent ? DeliveryStatus.Sent : DeliveryStatus.Failed;
        notification.SentAt = anySent ? DateTime.UtcNow : null;
        notification.Error = anySent ? null : "Не удалось отправить push-уведомление ни на одно устройство.";
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Opt-out: строки в NotificationPreference нет → канал включён по умолчанию.</summary>
    private async Task<bool> IsChannelEnabledAsync(Guid userId, NotificationType type, NotificationChannel channel, CancellationToken ct)
    {
        var pref = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type && p.Channel == channel, ct);
        return pref?.Enabled ?? true;
    }
}
