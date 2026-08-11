namespace GameOrg.Api.Features.Notifications;

/// <summary>Token — целиком JSON PushSubscription из браузера ({endpoint, keys:{p256dh, auth}}), не opaque-строка.</summary>
public sealed record RegisterDeviceRequest(string Token, string? Locale);

public sealed record UnregisterDeviceRequest(string Token);
