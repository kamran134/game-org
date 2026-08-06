namespace GameOrg.Domain.Entities;

/// <summary>
/// Transactional outbox: доменное событие пишется в одной транзакции с
/// изменением данных, релей отдаёт его в Hangfire. Гарантия at-least-once
/// без распределённых транзакций — и точка разреза монолита в будущем.
/// </summary>
public sealed class OutboxEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Event, Club, User...</summary>
    public required string AggregateType { get; set; }
    public required string AggregateId { get; set; }
    /// <summary>event.participant.joined...</summary>
    public required string Type { get; set; }
    public required Dictionary<string, object> Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
