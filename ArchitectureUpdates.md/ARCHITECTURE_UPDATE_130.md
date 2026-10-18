# ZArchitecture 130 – Inicializace schématu Events

## Příčina

CoreDatabaseHostedService volá `EnsureCreatedAsync`. Pokud PostgreSQL databáze už obsahuje tabulky, EF Core nevytvoří nově připojené tabulky modulů. `HostDbContext` přitom `ConfigureEvents()` zavolá správně.

## Oprava

- Nový `src/Apps/Events/EventsDatabaseHostedService.cs` registrovaný prostřednictvím `EventsModule.ConfigureServices`.
- Na PostgreSQL provede v jedné transakci idempotentní `CREATE TABLE IF NOT EXISTS` pro sedm doménových tabulek, vytvoření indexů a `INSERT ... ON CONFLICT DO NOTHING` pro pevné UUID.
- Na souběžných instancích serializuje bootstrap přes transakční advisory lock.
- Nevytváří ani nemaže žádné tabulky Core/DigitalProcesses, nepoužívá `EnsureDeleted`.
- SQLite prostředí nadále využívá `EnsureCreated` při vzniku nové databáze. Pro rozšíření **existující** SQLite databáze je potřebná migrace.

## Omezení

Jde o cílený PostgreSQL bootstrap počátečního schématu Events, nikoliv obecný migrační systém pro budoucí změny schématu. U již existujících tabulek se nekontrolují změněné sloupce – pro ty mají být nasazeny EF migrations.

## Ověření

`dotnet build src/Apps/Host/Host.csproj`

`ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug`

PostgreSQL: `SELECT tablename FROM pg_tables WHERE schemaname='public' AND (tablename LIKE 'event_%' OR tablename='events') ORDER BY tablename;`

V tomto pracovním prostředí není .NET SDK; kompilace a integrační testy nejsou potvrzeny.
