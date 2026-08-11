using GameOrg.Domain;

namespace GameOrg.Api.Features.Payments;

/// <summary>Список организатора события — видно только создателю/модератору (чужие финансовые данные).</summary>
public sealed record PaymentDto(
    Guid Id,
    Guid PayerId,
    string PayerDisplayName,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime? DueAt,
    DateTime? PaidAt,
    string? Note);

/// <summary>Встраивается в EventDetailDto.MyPayment — только собственный платёж участника.</summary>
public sealed record PaymentSummaryDto(Guid Id, decimal Amount, string Currency, PaymentMethod Method, PaymentStatus Status, DateTime? DueAt);

/// <summary>Для /api/me/payments — платежи пользователя по всем событиям.</summary>
public sealed record MyPaymentDto(
    Guid Id,
    Guid? EventId,
    string? EventPublicId,
    string? EventTitle,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTime? DueAt);

/// <summary>Method — только при переходе в Paid (чем реально заплатили). Note — свободный комментарий организатора.</summary>
public sealed record UpdatePaymentStatusRequest(PaymentStatus Status, PaymentMethod? Method, string? Note);
