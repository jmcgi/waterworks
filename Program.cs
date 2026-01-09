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
        var users = await db.Users.AsNoTracking().ToListAsync();
        var user = users.FirstOrDefault(u => u.PersonalCode == code);
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
        var users = await db.Users.AsNoTracking().ToListAsync();
        var user = users.FirstOrDefault(u => u.PersonalCode == personalCode);
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
        var bills = await db.Bills.AsNoTracking().ToListAsync();
        decimal sum = 0;
        foreach (var bill in bills)
        {
            if (bill.UserId == id && bill.Status == "unpaid" && decimal.TryParse(bill.Amount, out var amt))
                sum += amt;
        }
        return Results.Ok(new {
            userId = id,
            unpaidBalance = sum,
            unpaidBalanceWords = NumberToLtWords(sum)
        });
    }
    catch (Exception ex)
    {
        var root = ex.GetBaseException();
        return Results.Problem(statusCode: 500, title: "balance-error", detail: root.Message);
    }
});

// Lithuanian number-to-words for euros/centai (demo, covers 0-9999.99)
static string NumberToLtWords(decimal amount)
{
    string[] units = { "nulis", "vienas", "du", "trys", "keturi", "penki", "šeši", "septyni", "aštuoni", "devyni" };
    string[] teens = { "dešimt", "vienuolika", "dvylika", "trylika", "keturiolika", "penkiolika", "šešiolika", "septyniolika", "aštuoniolika", "devyniolika" };
    string[] tens = { "", "dešimt", "dvidešimt", "trisdešimt", "keturiasdešimt", "penkiasdešimt", "šešiasdešimt", "septyniasdešimt", "aštuoniasdešimt", "devyniasdešimt" };
    string[] hundreds = { "", "šimtas", "du šimtai", "trys šimtai", "keturi šimtai", "penki šimtai", "šeši šimtai", "septyni šimtai", "aštuoni šimtai", "devyni šimtai" };

    int euros = (int)amount;
    int cents = (int)((amount - euros) * 100);

    string ToWords(int n)
    {
        if (n == 0) return units[0];
        var parts = new List<string>();
        if (n >= 100)
        {
            parts.Add(hundreds[n / 100]);
            n %= 100;
        }
        if (n >= 20)
        {
            parts.Add(tens[n / 10]);
            n %= 10;
        }
        if (n >= 10)
        {
            parts.Add(teens[n - 10]);
            n = 0;
        }
        if (n > 0)
        {
            parts.Add(units[n]);
        }
        return string.Join(" ", parts);
    }

    string euroWord = euros == 1 ? "euras" : (euros % 10 >= 2 && euros % 10 <= 9 && (euros % 100 < 10 || euros % 100 >= 20) ? "eurai" : "eurų");
    string centWord = cents == 1 ? "centas" : (cents % 10 >= 2 && cents % 10 <= 9 && (cents % 100 < 10 || cents % 100 >= 20) ? "centai" : "centų");

    string result = "";
    if (euros > 0)
        result += ToWords(euros) + " " + euroWord;
    if (cents > 0)
        result += (result.Length > 0 ? " " : "") + ToWords(cents) + " " + centWord;
    if (result == "")
        result = "nulis eurų";
    return result.Trim();
}
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
