# SQLite DateTimeOffset: oprava zpětného čtení

Tento patch opravuje `System.FormatException` při čtení starších SQLite hodnot, např. `2026-10-10 11:23:25.0792985+00:00`.

- SQLite ukládá **nové** hodnoty jako normalizované UTC ISO 8601 (`O`).
- SQLite čte pomocí `DateTimeOffset.Parse(InvariantCulture)`, což zvládá původní textový formát i `O`.
- PostgreSQL a SQL Server zůstávají beze změny.
- Pozor: starší textové hodnoty **nejsou automaticky migrovány**. Jejich lexikografické řazení ve smíšeném formátu nemusí být chronologicky správné. Pro perzistentní SQLite DB je nutná jednorázová datová migrace; pro testy se stará data často vytvářejí přes SQL seed při bootstrapu.

Instalace v `/workspace`:

```bash
unzip -o ZArchitecture78_sqlite_utc_legacy_read_fix.zip -d /workspace
dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug
```

Netestováno běžícím .NET SDK (není dostupné).
