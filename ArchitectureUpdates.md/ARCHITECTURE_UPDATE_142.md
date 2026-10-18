# ARCHITECTURE_UPDATE_142 — oprava duplicitní kompilace Events

## Příčina
Po rozdělení Events na samostatné projekty `Events.Models`, `Events.Services`,
`Events.Resolvers` a `Events.UI` používal kořenový `Events.csproj` výchozí
rekurzivní zahrnování `**/*.cs`. Kompiloval tedy znovu typy obsažené
v referencovaných DLL. Projevilo se to mnoha warningy CS0436 a chybou CS0121
při volání `EventsModelBuilder.ConfigureEvents()`.

## Oprava
Soubor `src/Apps/Events/Events.csproj`:
- `EnableDefaultCompileItems=false`;
- explicitní seznam `Compile Include`: `EventsModule.cs`,
  `EventsDatabaseHostedService.cs`;
- projektové reference na jednotlivé vrstvy zachovány.

## Dopady
- Žádná změna API ani databázového schématu.
- Žádná migrace a žádné opakování importu.
- Kompilace a runtime vyžadují ověření v prostředí s .NET SDK.

## Ověření
- XML projektových souborů ověřeno parserem.
- Archiv ZIP ověřen pomocí testzip.
- Skutečný `dotnet build` nebylo možné provést (SDK zde není dostupné).
