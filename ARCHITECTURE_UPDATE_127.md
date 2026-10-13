# ZArchitecture 127 — Oprava CS1003 v EventsModelBuilder

## Oprava

V `src/Apps/Events/EventsModelBuilder.cs` byl řetězec SQL podmínky pro `HasCheckConstraint` chybně uzavřen pomocí běžných uvozovek, přestože obsahoval další uvozovky kolem názvů sloupců. Kompilátor proto hlásil osm chyb `CS1003` na řádku 13.

SQL výraz je nyní C# raw string literal (`"""..."""`). Logika CHECK constraintu se nemění: v `event_participants` musí být vyplněn právě jeden z `UserId` a `GroupId`.

## Ověření

- Ověřeno, že po změně je řetězec platný raw literal a archiv ZIP je čitelný.
- `.NET SDK` není ve zdejším prostředí instalováno, `dotnet build` ani `dotnet run` proto nebylo možné spustit.

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```
