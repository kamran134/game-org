namespace GameOrg.Domain.Entities;

/// <summary>
/// Идемпотентность мутирующих HTTP-запросов (двойной тап в TG WebApp,
/// ретраи бота). Клиент шлёт Idempotency-Key.
/// </summary>
public sealed class IdempotencyKey
{
    public required string Key { get; set; }
    public Guid? UserId { get; set; }
    public required string Endpoint { get; set; }
    public required string RequestHash { get; set; }
    public Dictionary<string, object>? ResponseBody { get; set; }
    public int? StatusCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
