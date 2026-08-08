using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace GameOrg.Infrastructure.Storage;

/// <summary>
/// R2-совместимый S3-клиент (Cloudflare R2). Настройки — плоские
/// env-переменные (R2_ACCOUNT_ID/R2_ACCESS_KEY_ID/R2_SECRET_ACCESS_KEY/
/// R2_BUCKET/R2_PUBLIC_URL), тот же паттерн, что JWT_SIGNING_KEY.
/// </summary>
public sealed class R2StorageService
{
    private readonly AmazonS3Client? _client;
    private readonly string _bucket;
    private readonly string _publicUrl;

    public R2StorageService(IConfiguration configuration)
    {
        _bucket = configuration["R2_BUCKET"] ?? "game-org-media";
        _publicUrl = (configuration["R2_PUBLIC_URL"] ?? string.Empty).TrimEnd('/');

        var endpoint = configuration["R2_ENDPOINT"];
        var accessKey = configuration["R2_ACCESS_KEY_ID"];
        var secretKey = configuration["R2_SECRET_ACCESS_KEY"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            // R2 ещё не настроен (нет бакета/токена — см. docs/PLAN.md §11.2,
            // отдельный ручной шаг пользователя). Не валим старт приложения —
            // эндпоинты, которым нужен R2, вернут понятную ошибку при обращении,
            // как TELEGRAM_BOT_TOKEN на Шаге 5.
            _client = null;
            return;
        }

        _client = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
        });
    }

    public bool IsConfigured => _client is not null;

    public Task<string> GetPresignedUploadUrlAsync(string key, string contentType, TimeSpan ttl)
    {
        if (_client is null)
            throw new InvalidOperationException("R2 не настроен: заданы не все R2_ACCOUNT_ID/R2_ACCESS_KEY_ID/R2_SECRET_ACCESS_KEY.");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = contentType,
        };

        return _client.GetPreSignedURLAsync(request);
    }

    public string GetPublicUrl(string key) => $"{_publicUrl}/{key}";
}
