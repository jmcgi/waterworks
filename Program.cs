using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using KlaipedosVandenysDemo.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
// NOTE: Do not hard-bind URLs here for Railway.
// Railway/.NET images typically provide the correct listener configuration via env vars,
// and overriding it can cause "address already in use" and crash-loop deploys.

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Resolve Postgres connection string
string resolvedConn = ConnectionStringHelper.ResolvePostgresConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(resolvedConn));
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// Simple liveness endpoint (no DB)
app.MapGet("/health/ready", () => Results.Ok(new { status = "ok" }));

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

static class ConnectionStringHelper
{
    public static string ResolvePostgresConnectionString(ConfigurationManager config)
    {
        var fromConnSection = config.GetConnectionString("DefaultConnection");
        var fromEnvConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? config["DATABASE_URL"]
                          ?? fromEnvConn
                          ?? fromConnSection
                          ?? string.Empty;

        if (string.IsNullOrWhiteSpace(databaseUrl))
            return databaseUrl;

        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return BuildNpgsqlConnectionStringFromUrl(databaseUrl);
        }

        return databaseUrl;
    }

    private static string BuildNpgsqlConnectionStringFromUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? "");
        var pass = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? "");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = user,
            Password = pass,
            Database = uri.AbsolutePath.TrimStart('/')
        };

        // Enforce SSL commonly required by managed Postgres providers
        builder.SslMode = SslMode.Require;

        return builder.ConnectionString;
    }
}
