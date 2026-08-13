using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using GameOrg.Api.Features.Clubs;
using GameOrg.Api.Features.Events;
using GameOrg.Api.Features.Geography;
using GameOrg.Api.Features.Identity;
using GameOrg.Api.Features.Moderation;
using GameOrg.Api.Features.Notifications;
using GameOrg.Api.Features.Payments;
using GameOrg.Api.Features.Profiles;
using GameOrg.Api.Features.Reputation;
using GameOrg.Api.Features.Social;
using GameOrg.Api.Features.Sports;
using GameOrg.Api.Features.Venues;
using GameOrg.Domain;
using GameOrg.Infrastructure;
using GameOrg.Infrastructure.Notifications;
using GameOrg.Infrastructure.Seed;
using GameOrg.Infrastructure.Storage;
using Hangfire;
using Hangfire.PostgreSql;
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
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<ClubService>();
builder.Services.AddScoped<FollowService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<RatingService>();

builder.Services.AddHttpClient<TelegramSender>();
builder.Services.AddSingleton<WebPushSender>();
builder.Services.AddScoped<NotificationSender>();
builder.Services.AddScoped<EventReminderJob>();

// Recurring напоминания за 24ч/2ч до события (EventReminderJob) — своя
// схема (hangfire.*) в той же Postgres, что и EF; не Redis (docs/PLAN.md §10).
// Дашборд (/hangfire) сознательно не подключаем — нет механизма авторизации
// для него, открывать наружу без неё нельзя.
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

// JWT читается либо из Authorization-заголовка (на будущее — mobile), либо из
// httpOnly cookie go_access (веб). Имя signing key совпадает с TokenService —
// см. TokenService.CreateAccessToken.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Без этого встроенный маппинг ASP.NET Core подменяет "sub" на
        // ClaimTypes.NameIdentifier (длинный XML-namespace URI) — тогда
        // principal.FindFirstValue(JwtRegisteredClaimNames.Sub) в
        // ProfileEndpoints/VenuesEndpoints ничего не находит, даже с
        // валидным токеном (RequireAuthorization проходит, а GetUserId — нет).
        options.MapInboundClaims = false;
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
            // Та же ловушка, что с "sub": по умолчанию ASP.NET Core ищет роль
            // в длинном ClaimTypes.Role URI, а токен несёт короткий "role"
            // (TokenService.CreateAccessToken) — без этого RequireRole никогда
            // не находит claim, даже у валидного токена с нужной ролью.
            RoleClaimType = "role",
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
// Admin может всё, что может Moderator — Moderator ⊂ Admin (docs/PLAN.md, Шаг 9).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Moderator", policy => policy.RequireRole(nameof(UserRole.Moderator), nameof(UserRole.Admin)));
    options.AddPolicy("Admin", policy => policy.RequireRole(nameof(UserRole.Admin)));
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
app.UseAuthentication();
app.UseAuthorization();

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    version,
    env = app.Environment.EnvironmentName,
}));

// Статический RecurringJob.AddOrUpdate падает: JobStorage.Current
// инициализируется не сразу после AddHangfire(...), а позже (в момент,
// когда сам DI-контейнер резолвит связанные сервисы) — на старте его ещё
// нет, и статический API кидает необработанное исключение, роняя весь
// процесс. IRecurringJobManager из DI не зависит от этого глобального
// состояния — тот же метод, но через контейнер.
using (var recurringJobScope = app.Services.CreateScope())
{
    recurringJobScope.ServiceProvider.GetRequiredService<IRecurringJobManager>().AddOrUpdate<EventReminderJob>(
        "event-reminders",
        job => job.SendDueRemindersAsync(CancellationToken.None),
        "*/10 * * * *");
}

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapSportsEndpoints();
app.MapCitiesEndpoints();
app.MapVenuesEndpoints();
app.MapEventsEndpoints();
app.MapModerationEndpoints();
app.MapAdminEndpoints();
app.MapNotificationsEndpoints();
app.MapClubsEndpoints();
app.MapSocialEndpoints();
app.MapReputationEndpoints();
app.MapPaymentsEndpoints();

app.Run();

public partial class Program; // видимость для WebApplicationFactory в интеграционных тестах
