using System.Reflection;
using System.Threading.RateLimiting;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Sports;
using GameOrg.Api.Features.Venues;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// JSON-логи, когда stdout уходит в Docker/systemd (нет интерактивного терминала —
// т.е. и на dev-сервере, и на проде); читаемый текст — при локальном `dotnet watch run`.
var loggerConfig = new LoggerConfiguration().Enrich.FromLogContext();
loggerConfig = Console.IsOutputRedirected
    ? loggerConfig.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    : loggerConfig.WriteTo.Console();
Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<GameOrgDbContext>(options => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.UseNetTopologySuite())
    .UseSnakeCaseNamingConvention());

// Как в текущем боте (src/webapp/server.ts) — 60 запросов/мин на IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

// Только известные origin'ы из конфигурации — см. Cors:AllowedOrigins в appsettings.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ASPNETCORE_ENVIRONMENT=Development одинаков и на локальной машине, и на dev-сервере
// (см. docs/PLAN.md §5) — поэтому автоприменение миграций гоняется по отдельному
// явному флагу, а не по имени окружения. Локально — false (миграции вручную,
// `dotnet ef database update`); на dev-сервере compose выставляет AUTO_MIGRATE=true.
if (builder.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var migrateScope = app.Services.CreateScope();
    await migrateScope.ServiceProvider.GetRequiredService<GameOrgDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    // Справочники (виды спорта, города, ачивки) — идемпотентно, безопасно на каждом старте.
    using var seedScope = app.Services.CreateScope();
    await DataSeeder.SeedAsync(seedScope.ServiceProvider.GetRequiredService<GameOrgDbContext>());
}

app.UseHttpsRedirection();
app.UseCors("Default");
app.UseRateLimiter();

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    version,
    env = app.Environment.EnvironmentName,
}));

app.MapSportsEndpoints();
app.MapCitiesEndpoints();
app.MapVenuesEndpoints();

app.Run();

public partial class Program; // видимость для WebApplicationFactory в интеграционных тестах
