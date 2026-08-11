using GameOrg.Api.Common;
using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Payments;

/// <summary>
/// Только офлайн-методы (Cash/BankTransfer/Balance) — организатор вручную отмечает
/// получение денег, без внешнего платёжного шлюза (docs/PLAN.md, Шаг 18). CardOnline
/// (epoint/payriff/stripe) — отдельный, не запланированный здесь шаг.
/// </summary>
public sealed class PaymentService(GameOrgDbContext db, NotificationSender notificationSender)
{
    /// <summary>
    /// Создаёт Pending-платёж участнику, ставшему Confirmed — только если у события
    /// не Free и у участника есть UserId (гостям выставлять не на кого — PayerId
    /// в домене обязателен). Идемпотентно — повторный вызов на уже существующего
    /// участника ничего не делает.
    /// </summary>
    public async Task EnsurePaymentAsync(Guid eventId, Guid participantId, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null || ev.CostSplit == CostSplit.Free || ev.Cost is null) return;

        var participant = await db.EventParticipants.FirstOrDefaultAsync(p => p.Id == participantId, ct);
        if (participant?.UserId is null) return;

        var exists = await db.Payments.AnyAsync(p => p.ParticipantId == participantId, ct);
        if (exists) return;

        var amount = ComputeAmount(ev);
        if (amount is null) return;

        var payment = new Payment
        {
            EventId = ev.Id,
            ParticipantId = participant.Id,
            ClubId = ev.ClubId,
            PayerId = participant.UserId.Value,
            Amount = amount.Value,
            Currency = ev.Currency,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Pending,
            DueAt = ev.RegistrationClosesAt ?? ev.StartsAt,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        await notificationSender.SendAsync(
            participant.UserId.Value,
            NotificationType.PaymentDue,
            $"PAYMENT_DUE:{payment.Id}",
            $"К оплате {payment.Amount} {payment.Currency} за событие.",
            new Dictionary<string, object> { ["eventId"] = eventId.ToString(), ["paymentId"] = payment.Id.ToString() },
            ct);
    }

    /// <summary>PerPlayer — фиксированная сумма с человека. Total — Cost / ConfirmedCount, копейка расхождения при неровном делении не решается.</summary>
    private static decimal? ComputeAmount(Event ev) => ev.CostSplit switch
    {
        CostSplit.PerPlayer => ev.Cost,
        CostSplit.Total => ev.ConfirmedCount > 0 ? Math.Round(ev.Cost!.Value / ev.ConfirmedCount, 2) : ev.Cost,
        _ => null,
    };

    /// <summary>Зовётся после любого изменения состава (join/leave/повышение) — только для Total, только ещё не оплаченные.</summary>
    public async Task RecomputeTotalSplitAsync(Guid eventId, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null || ev.CostSplit != CostSplit.Total || ev.Cost is null) return;

        var amount = ev.ConfirmedCount > 0 ? Math.Round(ev.Cost.Value / ev.ConfirmedCount, 2) : ev.Cost.Value;
        var now = DateTime.UtcNow;
        await db.Payments
            .Where(p => p.EventId == eventId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Amount, amount).SetProperty(p => p.UpdatedAt, now), ct);
    }

    public Task CancelPendingForParticipantAsync(Guid participantId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.Payments
            .Where(p => p.ParticipantId == participantId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Cancelled).SetProperty(p => p.UpdatedAt, now), ct);
    }

    public Task CancelAllPendingForEventAsync(Guid eventId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.Payments
            .Where(p => p.EventId == eventId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Cancelled).SetProperty(p => p.UpdatedAt, now), ct);
    }

    /// <summary>Только создатель события или модератор — чужие финансовые данные.</summary>
    public async Task<(List<PaymentDto>? Result, string? Error)> GetForEventAsync(
        Guid eventId, Guid viewerId, bool isModerator, string locale, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (ev is null) return (null, "Событие не найдено.");
        if (ev.CreatedById != viewerId && !isModerator) return (null, "Список платежей виден только создателю события или модератору.");

        var payments = await db.Payments
            .Where(p => p.EventId == eventId)
            .Include(p => p.Payer)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

        return (payments.Select(p => new PaymentDto(
            p.Id, p.PayerId, Localized.Resolve(p.Payer.DisplayNameI18n, locale) ?? p.Payer.Handle,
            p.Amount, p.Currency, p.Method, p.Status, p.DueAt, p.PaidAt, p.Note)).ToList(), null);
    }

    /// <summary>На Paid шлёт PaymentConfirmed. Refund/повторная отмена — тот же эндпоинт, организатор сам меняет статус.</summary>
    public async Task<(bool Ok, string? Error)> SetStatusAsync(Guid paymentId, Guid actorId, bool isModerator, UpdatePaymentStatusRequest request, CancellationToken ct)
    {
        var payment = await db.Payments.Include(p => p.Event).FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null) return (false, "Платёж не найден.");
        if (payment.Event?.CreatedById != actorId && !isModerator) return (false, "Изменить статус платежа может только создатель события или модератор.");

        payment.Status = request.Status;
        if (request.Method is not null) payment.Method = request.Method.Value;
        if (request.Note is not null) payment.Note = request.Note;
        payment.UpdatedAt = DateTime.UtcNow;

        if (request.Status == PaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;
            payment.ConfirmedById = actorId;
        }

        await db.SaveChangesAsync(ct);

        if (request.Status == PaymentStatus.Paid)
        {
            await notificationSender.SendAsync(
                payment.PayerId,
                NotificationType.PaymentConfirmed,
                $"PAYMENT_CONFIRMED:{payment.Id}:{payment.UpdatedAt.Ticks}",
                $"Оплата {payment.Amount} {payment.Currency} подтверждена.",
                new Dictionary<string, object> { ["paymentId"] = payment.Id.ToString() },
                ct);
        }

        return (true, null);
    }

    public async Task<List<MyPaymentDto>> GetMyPaymentsAsync(Guid userId, string locale, CancellationToken ct)
    {
        var payments = await db.Payments
            .Where(p => p.PayerId == userId)
            .Include(p => p.Event)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return payments.Select(p => new MyPaymentDto(
            p.Id, p.EventId, p.Event?.PublicId, p.Event is null ? null : Localized.Resolve(p.Event.TitleI18n, locale),
            p.Amount, p.Currency, p.Method, p.Status, p.DueAt)).ToList();
    }

    public async Task<PaymentSummaryDto?> GetMyPaymentForEventAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.EventId == eventId && p.PayerId == userId, ct);
        return payment is null ? null : new PaymentSummaryDto(payment.Id, payment.Amount, payment.Currency, payment.Method, payment.Status, payment.DueAt);
    }
}
