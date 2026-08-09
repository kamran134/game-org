using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameOrg.Infrastructure.Notifications;

/// <summary>
/// Тонкая обёртка над Telegram Bot API sendMessage. Тот же TELEGRAM_BOT_TOKEN,
/// что уже используется для проверки Login Widget (Шаг 5) — тот же
/// плоский env-var паттерн, что у R2StorageService/JWT_SIGNING_KEY.
/// </summary>
public sealed class TelegramSender(HttpClient http, IConfiguration configuration, ILogger<TelegramSender> logger)
{
    private readonly string? _botToken = configuration["TELEGRAM_BOT_TOKEN"];

    public bool IsConfigured => !string.IsNullOrEmpty(_botToken);

    /// <summary>
    /// chatId — numeric Telegram user id (Account.ProviderUserId для
    /// Provider=Telegram). Возвращает false вместо исключения: юзер мог не
    /// открыть бота или заблокировать его — ожидаемый исход, не ошибка сервера,
    /// вызывающий код сам решает, что делать (пишет Notification.Status=Failed).
    /// </summary>
    public async Task<bool> TrySendAsync(string chatId, string text, CancellationToken ct)
    {
        if (!IsConfigured) return false;

        try
        {
            var response = await http.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_botToken}/sendMessage",
                new { chat_id = chatId, text },
                ct);

            if (response.IsSuccessStatusCode) return true;

            logger.LogWarning("Telegram sendMessage failed for chat {ChatId}: {Status}", chatId, response.StatusCode);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Telegram sendMessage threw for chat {ChatId}", chatId);
            return false;
        }
    }
}
