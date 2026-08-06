using System.Reflection;
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

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    version,
    env = app.Environment.EnvironmentName,
}));

app.Run();

public partial class Program; // видимость для WebApplicationFactory в интеграционных тестах
