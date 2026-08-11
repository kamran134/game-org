using System.Net;
using System.Text.Json;
using GameOrg.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace GameOrg.Infrastructure.Notifications;

/// <summary>
/// Тонкая обёртка над Web Push API (VAPID) — тот же плоский env-var паттерн,
/// что R2StorageService/TelegramSender. DeviceToken.Token хранит целиком JSON
/// PushSubscription ({endpoint, keys:{p256dh, auth}}) — так его отдаёт браузер,
/// заводить отдельные колонки под endpoint/p256dh/auth не нужно (docs/PLAN.md, Шаг 17).
/// </summary>
public sealed class WebPushSender
{
    private readonly WebPushClient _client = new();
    private readonly VapidDetails? _vapidDetails;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(IConfiguration configuration, ILogger<WebPushSender> logger)
    {
        _logger = logger;
        var publicKey = configuration["VAPID_PUBLIC_KEY"];
        var privateKey = configuration["VAPID_PRIVATE_KEY"];
        var subject = configuration["VAPID_SUBJECT"];

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(subject))
        {
            _vapidDetails = null;
            return;
        }

        _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        VapidPublicKey = publicKey;
    }

    public bool IsConfigured => _vapidDetails is not null;

    /// <summary>Отдаётся фронту через GET /api/push/vapid-public-key — applicationServerKey для pushManager.subscribe.</summary>
    public string? VapidPublicKey { get; }

    /// <summary>
    /// Expired=true — подписка мертва (404/410 от push-сервиса), вызывающий код должен
    /// удалить DeviceToken. Любой другой сбой (сеть, невалидный VAPID) — просто Sent=false.
    /// </summary>
    public async Task<(bool Sent, bool Expired)> TrySendAsync(DeviceToken device, string title, string body, CancellationToken ct)
    {
        if (_vapidDetails is null) return (false, false);

        PushSubscriptionData? data;
        try
        {
            data = JsonSerializer.Deserialize<PushSubscriptionData>(device.Token);
        }
        catch (JsonException)
        {
            return (false, true); // битый токен — трактуем как протухший, чтобы самоочистился
        }

        if (data?.Endpoint is null || data.Keys?.P256dh is null || data.Keys.Auth is null)
            return (false, true);

        var subscription = new PushSubscription(data.Endpoint, data.Keys.P256dh, data.Keys.Auth);
        var payload = JsonSerializer.Serialize(new { title, body });

        try
        {
            await _client.SendNotificationAsync(subscription, payload, _vapidDetails, cancellationToken: ct);
            return (true, false);
        }
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return (false, true);
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "Web Push send failed for device {DeviceId}: {Status}", device.Id, ex.StatusCode);
            return (false, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Web Push send threw for device {DeviceId}", device.Id);
            return (false, false);
        }
    }

    private sealed record PushSubscriptionData(string? Endpoint, PushSubscriptionKeys? Keys);

    private sealed record PushSubscriptionKeys(string? P256dh, string? Auth);
}
