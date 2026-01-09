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

// Resolve Postgres connection string (DATABASE_URL preferred)
string resolvedConn = ConnectionStringHelper.ResolvePostgresConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(resolvedConn));

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
app.MapGet("/api/users/by-personal-code/{code}", async (string code, AppDbContext db) =>
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

app.MapGet("/user/{personalCode}", async (string personalCode, AppDbContext db) =>
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

// Endpoint: GET /api/users/{id}/balance
// Returns sum of unpaid bills for user
app.MapGet("/api/users/{id}/balance", async (string id, AppDbContext db) =>
{
    try
    {
        var bills = await db.Bills
            .AsNoTracking()
            .Where(b => b.UserId.ToString() == id && b.Status == "unpaid")
            .ToListAsync();
        decimal sum = 0;
        foreach (var bill in bills)
        {
            if (decimal.TryParse(bill.Amount.ToString(), out var amt))
                sum += amt;
        }
        return Results.Ok(new { userId = id, unpaidBalance = sum });
    }
    catch (Exception ex)
    {
        var root = ex.GetBaseException();
        return Results.Problem(statusCode: 500, title: "balance-error", detail: root.Message);
    }
});
app.Run();

static class ConnectionStringHelper
{
    public static string ResolvePostgresConnectionString(ConfigurationManager config)
    {
        // Prefer DATABASE_URL (Railway standard), then ConnectionStrings.
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                          ?? config["DATABASE_URL"]
                          ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                || databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return BuildNpgsqlConnectionStringFromUrl(databaseUrl);
            }

            // Already a normal Npgsql connection string.
            return databaseUrl;
        }

        var fromEnvConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromEnvConn))
            return fromEnvConn;

        return config.GetConnectionString("DefaultConnection") ?? string.Empty;
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

        // Railway internal networking may not require SSL; external managed DBs often do.
        builder.SslMode = uri.Host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase)
            ? SslMode.Prefer
            : SslMode.Require;

        return builder.ConnectionString;
    }
}
