# ZArchitecture78 — PostgreSQL-only main application and integration tests

The Host now uses `UseNpgsql` and reads `ConnectionStrings:DefaultConnection` from `src/Host/appsettings.Development.json`.

- Development: `postgres_development`, database `data`.
- Test administrator: `postgres_test`, database `data`, read from `Host/appsettings.Development.json` (`IntegrationTests`). Each integration fixture creates its own `za78_<guid>` database on this server and drops it when disposed. Requires CREATEDB rights (postgres superuser supplied by Compose).
- `src/Host/appsettings.Development.json` contains local-only sample credentials. Do not use them in production.
- Test isolation refuses to connect to hosts other than `postgres_test`.
- Ensure `ASPNETCORE_ENVIRONMENT=Development` in your Dev Container.
- Running: `dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug`.
- Also run `tests/Core.SecurityLayer.Tests` and `tests/Core/Host.Integration.Tests`.
- The old SQLite dump/restore and schema-update paths are disabled in the active PostgreSQL Host. Postgres schema currently uses EF `EnsureCreated` for ephemeral databases; introduce EF migrations before changing persistent schema in development/production.
- Some legacy standalone SQLite helpers under `src/Core/Base/Security` are not part of EF Core DBLayer. Those are left as separate legacy components; the Host and test fixtures use PostgreSQL exclusively.
- No .NET SDK available in the build environment; source edits and archive can be inspected but test compilation must be confirmed in Dev Container.
