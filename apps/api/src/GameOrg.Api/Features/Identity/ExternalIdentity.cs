using GameOrg.Domain;

namespace GameOrg.Api.Features.Identity;

/// <summary>
/// Нормализованная форма личности от внешнего провайдера. Единственная точка
/// контакта между провайдер-специфичным валидатором (сейчас — Telegram, в
/// будущем Google/Apple) и провайдер-агностичной логикой входа/регистрации.
/// </summary>
public sealed record ExternalIdentity(
    AuthProvider Provider,
    string ProviderUserId,
    string? Email,
    string DisplayName,
    string? AvatarUrl);
