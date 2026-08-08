using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Identity;
using GameOrg.Api.Features.Profiles;
using GameOrg.Api.Features.Sports;
using GameOrg.Api.Features.Venues;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Seed;
using GameOrg.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// До Шага 6 в ответах API не было ни одного enum-поля — без этого
// Gender/SkillLevel/Footedness/Visibility ушли бы в JSON как голые числа.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
// AllowCredentials — web и api на dev/prod ходят через разные origin'ы (nginx
// path-роутинг не защищает localhost), cookies с токенами иначе не долетят.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddSingleton<R2StorageService>();
builder.Services.AddScoped<VenueService>();

// JWT читается либо из Authorization-заголовка (на будущее — mobile), либо из
// httpOnly cookie go_access (веб). Имя signing key совпадает с TokenService —
// см. TokenService.CreateAccessToken.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = TokenService.Issuer,
            ValidAudience = TokenService.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["JWT_SIGNING_KEY"]
                ?? throw new InvalidOperationException("JWT_SIGNING_KEY не задан."))),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("go_access", out var token) && !string.IsNullOrEmpty(token))
                    context.Token = token;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

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
app.UseAuthentication();
app.UseAuthorization();

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    version,
    env = app.Environment.EnvironmentName,
}));

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapSportsEndpoints();
app.MapCitiesEndpoints();
app.MapVenuesEndpoints();

app.Run();

public partial class Program; // видимость для WebApplicationFactory в интеграционных тестах
