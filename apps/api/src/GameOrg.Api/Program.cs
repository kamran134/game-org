using System.Reflection;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
