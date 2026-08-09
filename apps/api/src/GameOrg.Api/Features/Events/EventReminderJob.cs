using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Events;

/// <summary>
/// Recurring job (Hangfire, каждые ~10 минут — см. Program.cs): напоминания
/// за 24ч и 2ч до начала события. DedupeKey в NotificationSender гарантирует
/// идемпотентность — событие, попавшее в окно на двух соседних тиках, не
/// получит напоминание дважды.
/// </summary>
public sealed class EventReminderJob(GameOrgDbContext db, NotificationSender notificationSender)
{
    // Шире периода джобы (10 мин) — чтобы не потерять событие, если тик
    // случился чуть позже расчётного времени.
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public async Task SendDueRemindersAsync(CancellationToken ct = default)
    {
        await SendForOffsetAsync(TimeSpan.FromHours(24), NotificationType.EventReminder24h, "24H", ct);
        await SendForOffsetAsync(TimeSpan.FromHours(2), NotificationType.EventReminder2h, "2H", ct);
    }

    private async Task SendForOffsetAsync(TimeSpan offset, NotificationType type, string label, CancellationToken ct)
    {
        var windowStart = DateTime.UtcNow + offset;
        var windowEnd = windowStart + Window;

        var events = await db.Events
            .Where(e => e.StartsAt >= windowStart && e.StartsAt < windowEnd)
            .Where(e => e.Status == EventStatus.Scheduled || e.Status == EventStatus.Confirmed)
            .Include(e => e.Participants)
            .ToListAsync(ct);

        foreach (var ev in events)
        {
            foreach (var participant in ev.Participants.Where(p => p.UserId is not null && p.Status == ParticipationStatus.Confirmed))
            {
                await notificationSender.SendAsync(
                    participant.UserId!.Value,
                    type,
                    $"EVENT_REMINDER_{label}:{ev.Id}:{participant.UserId}",
                    label == "24H" ? "Через 24 часа начинается событие." : "Через 2 часа начинается событие.",
                    new Dictionary<string, object> { ["eventId"] = ev.Id.ToString() },
                    ct);
            }
        }
    }
}
