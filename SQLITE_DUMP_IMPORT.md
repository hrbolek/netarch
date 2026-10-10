# Jednorázový přenos SQLite dump.json do PostgreSQL

Nový samostatný .NET nástroj `tools/SqliteDumpToPostgres` čte původní
`DatabaseDumpService` JSON formát 1. Není spojen se startem ani ukončením Hostu.

## Před importem

- PostgreSQL `postgres_development` musí mít již vytvořené tabulky aktuálního modelu
  (jednou spustit Host a poté jej zastavit).
- Zastavte běžící Host, který používá vývojovou databázi.
- Udělejte si zálohu databáze. Import **nahradí celý obsah všech aplikačních tabulek**.
- Soubor `dump.json` musí odpovídat schématu aktuální verze; nástroj kontroluje tabulky a sloupce.
- Spusťte v kořeni `/workspace`:

```bash
# Příklad zálohy (z lokálního počítače s Docker CLI)
docker compose -f .devcontainer/docker-compose.yml exec -T postgres_development \
  pg_dump -U postgres -d data -Fc > before_sqlite_import.dump

# Jednorázový import (z terminálu Dev Containeru)
dotnet run --project tools/SqliteDumpToPostgres -- \
  /workspace/dump.json --replace-development
```

Nástroj načítá `DefaultConnection` z `src/Host/appsettings.Development.json`,
odmítne jinou adresu než `postgres_development` a databázi než `data`.
Používá transakci, kontroluje integritu cizích klíčů a při chybě vrací původní data.
Vyžaduje PostgreSQL superusera pro `SET LOCAL session_replication_role = replica`.

Po dokončení spusťte Host. `DatabaseInitializer` doplní chybějící systémová data,
a pokud je `EffectiveAccess` prázdný, projekci přepočítá. Je-li v importu stará
nebo neúplná projekce, dočasně nastavte `SecurityProjection:RebuildOnStartup=true`
a po prvním startu konfiguraci vraťte.

Důležité: SQLite `dump.json` může obsahovat datové hodnoty, které PostgreSQL
nativně neakceptuje (např. nestandardní časové řetězce). Import pak zruší celou
transakci a oznámí chybu; neprovádí tichou ztrátovou konverzi dat.
