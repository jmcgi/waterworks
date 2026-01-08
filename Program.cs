using Microsoft.EntityFrameworkCore;
using KlaipedosVandenysDemo.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
// NOTE: Do not hard-bind URLs here for Railway.
// Railway/.NET images typically provide the correct listener configuration via env vars,
// and overriding it can cause "address already in use" and crash-loop deploys.

// Railway terminates TLS at the edge. If the runtime env accidentally includes an https URL
// (e.g., via ASPNETCORE_URLS), Kestrel will try to bind HTTPS and crash without a cert.
// In non-Development environments, force HTTP-only binding.
if (!builder.Environment.IsDevelopment())
{
    var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    if (!string.IsNullOrWhiteSpace(aspnetcoreUrls)
        && aspnetcoreUrls.Contains("https://", StringComparison.OrdinalIgnoreCase))
    {
        var httpOnly = string.Join(
            ';',
            aspnetcoreUrls
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrWhiteSpace(httpOnly))
        {
            var port = Environment.GetEnvironmentVariable("PORT");
            httpOnly = !string.IsNullOrWhiteSpace(port) ? $"http://0.0.0.0:{port}" : "http://0.0.0.0:8080";
        }

        builder.WebHost.UseUrls(httpOnly);
    }
}

// DEMO ONLY: hardcoded DB connection string (ignore env vars/config)
// NOTE: This is intentionally insecure; do not use in real deployments.
const string HardcodedPostgresConnectionString =
    "Host=ballast.proxy.rlwy.net;Port=23442;Database=railway;Username=postgres;Password=JFPcaNemJqABAaIzpuUCmvwPcMQhvlDp;Ssl Mode=Require;Trust Server Certificate=true";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(HardcodedPostgresConnectionString));

var app = builder.Build();

// Simple liveness endpoint (no DB)
app.MapGet("/health/ready", () => Results.Ok(new { status = "ok" }));

// DB connectivity check (helps distinguish network vs DB issues)
app.MapGet("/health/db", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "ok" })
            : Results.Problem(statusCode: 503, title: "db-unavailable");
    }
    catch (Exception ex)
    {
        return Results.Problem(statusCode: 500, title: "db-error", detail: ex.GetBaseException().Message);
    }
});

// Demo endpoint: return a user by personal code
// Keep the existing route used earlier, plus a short demo-friendly alias.
app.MapGet("/api/users/by-personal-code/{code:long}", async (long code, AppDbContext db) =>
{
    try
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PersonalCode == code);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
    catch (Exception ex)
    {
        var root = ex.GetBaseException();
        return Results.Problem(statusCode: 500, title: "lookup-failed", detail: root.Message);
    }
});

app.MapGet("/user/{personalCode:long}", async (long personalCode, AppDbContext db) =>
{
    try
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PersonalCode == personalCode);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
    catch (Exception ex)
    {
        var root = ex.GetBaseException();
        return Results.Problem(statusCode: 500, title: "lookup-failed", detail: root.Message);
    }
});
app.Run();

// (ConnectionStringHelper removed for demo simplicity)
