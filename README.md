# KlaipedosVandenysDemo (.NET 10 API)

Minimal ASP.NET Core Web API that connects to SQL Server database `KlaipedosVandenys` and exposes read-only endpoints to query users by personal code, email, surname, and phone.

## Prerequisites
- .NET SDK 10
- SQL Server (local default instance) with access to create databases
- Windows PowerShell

## Setup Database
Run these SQL scripts in SSMS or `sqlcmd`:

1. Create database:
   - File: sql/create_database.sql
2. Create `Users` table:
   - File: sql/create_users_table.sql

Optional `sqlcmd` example (run as Administrator if needed):

```powershell
sqlcmd -S localhost -E -i .\sql\create_database.sql
sqlcmd -S localhost -E -i .\sql\create_users_table.sql
```

## Configure Connection
`appsettings.json` already contains:

```
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=KlaipedosVandenys;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
Update for remote servers or SQL auth if required.

## Run the API
```powershell
cd c:\Users\janis.mikulionis\KlaipedosVandenysDemo
dotnet build
dotnet run
```

The API will start on https://localhost:7087 by default.

## Endpoints (GET)
- /api/users/by-personal-code/{code}
- /api/users/by-email/{email}
- /api/users/by-surname/{surname}
- /api/users/by-phone/{phone}

Examples:
```powershell
curl https://localhost:7087/api/users/by-email/test@example.com -k
curl https://localhost:7087/api/users/by-surname/Smith -k
```

## Notes
- Endpoints are read-only; no write operations.
- Uses EF Core 10 with SQL Server provider.
