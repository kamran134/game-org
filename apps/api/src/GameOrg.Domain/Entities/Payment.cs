namespace GameOrg.Domain.Entities;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }
    public Guid? ParticipantId { get; set; }
    public EventParticipant? Participant { get; set; }
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }

    public Guid PayerId { get; set; }
    public User Payer { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>epoint, payriff, stripe...</summary>
    public string? Provider { get; set; }
    public string? ExternalId { get; set; }

    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public User? ConfirmedBy { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
