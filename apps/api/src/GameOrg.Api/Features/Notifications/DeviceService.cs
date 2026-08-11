using GameOrg.Domain;
using GameOrg.Domain.Entities;
using GameOrg.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameOrg.Api.Features.Notifications;

/// <summary>Только Web (docs/PLAN.md, Шаг 17) — Ios/Android потребовали бы другой SDK (FCM/APNs), вне объёма.</summary>
public sealed class DeviceService(GameOrgDbContext db)
{
    public async Task RegisterAsync(Guid userId, RegisterDeviceRequest request, CancellationToken ct)
    {
        var device = await db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == request.Token, ct);
        if (device is null)
        {
            db.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                Platform = DevicePlatform.Web,
                Token = request.Token,
                Locale = request.Locale,
            });
        }
        else
        {
            // Тот же браузер мог перелогиниться другим пользователем на этом устройстве —
            // подписка теперь принадлежит текущему.
            device.UserId = userId;
            device.Locale = request.Locale;
            device.LastSeenAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task UnregisterAsync(Guid userId, string token, CancellationToken ct)
    {
        var device = await db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token && d.UserId == userId, ct);
        if (device is null) return;

        db.DeviceTokens.Remove(device);
        await db.SaveChangesAsync(ct);
    }
}
