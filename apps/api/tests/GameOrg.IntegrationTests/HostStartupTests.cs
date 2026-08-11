using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace GameOrg.IntegrationTests;

/// <summary>
/// Поднимает реальный хост приложения. Ошибки маппинга роутов (например,
/// MapDelete с параметром, для которого фреймворк не может вывести биндинг
/// тела) роняют весь хост при старте — ASP.NET Core валидирует таблицу
/// эндпоинтов эагерли при первом построении AuthorizationPolicyCache.
/// dotnet build и юнит-тесты этого не ловят: сборка типизирована корректно,
/// ошибка чисто рантаймовая. Нужен живой Postgres — Hangfire подключается
/// к БД уже при builder.Build(), до всякой миграции/сидинга.
/// </summary>
public sealed class HostStartupTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => _db.StartAsync();

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    [Fact]
    public async Task AppStartsAndHealthEndpointResponds()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _db.GetConnectionString(),
                ["JWT_SIGNING_KEY"] = "test_signing_key_min_32_characters_long_ok",
            })));

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode, $"/health вернул {(int)response.StatusCode}");
    }
}
