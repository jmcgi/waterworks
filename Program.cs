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

// Resolve Postgres connection string
string resolvedConn = ConnectionStringHelper.ResolvePostgresConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(resolvedConn));

var app = builder.Build();

// Simple liveness endpoint (no DB)
app.MapGet("/health/ready", () => Results.Ok(new { status = "ok" }));

// Demo endpoint: return a user by personal code
// Keep the existing route used earlier, plus a short demo-friendly alias.
app.MapGet("/api/users/by-personal-code/{code:long}", async (long code, AppDbContext db) =>
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PersonalCode == code);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapGet("/user/{personalCode:long}", async (long personalCode, AppDbContext db) =>
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PersonalCode == personalCode);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});
app.Run();

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
