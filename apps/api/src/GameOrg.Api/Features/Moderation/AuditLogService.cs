using System.Text.Json;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;

namespace GameOrg.Api.Features.Moderation;

/// <summary>Пишет факты модерации (кто/что/до/после), не проверяет права — это дело вызывающего кода.</summary>
public sealed class AuditLogService(GameOrgDbContext db)
{
    public async Task LogAsync(
        Guid? actorId, string action, string entityType, Guid entityId,
        object? before, object? after, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Before = ToDict(before),
            After = ToDict(after),
        });
        await db.SaveChangesAsync(ct);
    }

    private static Dictionary<string, object>? ToDict(object? value) =>
        value is null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(value));
}
