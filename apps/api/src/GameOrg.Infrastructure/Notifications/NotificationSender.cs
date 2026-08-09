using GameOrg.Domain;
using GameOrg.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Infrastructure.Notifications;

/// <summary>
/// Общая точка для "написать Notification и сразу попытаться отправить" —
/// используется и немедленной отправкой (EventService: отмена события,
/// повышение из вейтлиста), и плановой (EventReminderJob). Идемпотентно по
/// DedupeKey: если запись с таким ключом уже есть, второй раз не шлёт.
/// </summary>
public sealed class NotificationSender(GameOrgDbContext db, TelegramSender telegramSender)
{
    public async Task SendAsync(
        Guid userId, NotificationType type, string dedupeKey, string text, Dictionary<string, object>? data, CancellationToken ct)
    {
        var exists = await db.Notifications.AnyAsync(n => n.DedupeKey == dedupeKey, ct);
        if (exists) return;

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
}
