# ZArchitecture 118 — jednoduchý životní cyklus IModule

## Změny

- Nové `IModule` s fázemi `ConfigureServices`, `ConfigureDatabase`, `ConfigureApplication`, `ConfigureNavigation`, `MapAPI`, `MapUI`. Nepoužité fáze mají výchozí prázdnou implementaci.
- `ModuleMode.Api` / `ModuleMode.Ui`: Host vybírá jedno mapování za modul, nikdy obě současně.
- `ArchitectureModuleExtensions` registruje moduly v DI a poskytuje `ConfigureModules()` a `MapModules()`.
- `CoreModule` a `DigitalProcessesModule` převedeny na `IModule`; starší `MapEndpoints` zachováno kvůli existujícímu kódu.
- Registrace HTTP middleware přes `CoreModule.ConfigureApplication`; Host volá fáze v pořadí DI → Build → pipeline → endpointy.
- `HostDbContext` při sestavování modelu volá EF příspěvky z každého `IModule`, současně zachovává staré `IAppModelContributor`.
- Šablona `ApplicationTemplate` generuje konkrétní modul podle kontraktu `IModule`; stávající soubor EF contributor využívá přímo.
- `IArchitectureModule` ponecháno jako kompatibilní zastaralé rozhraní.

## Záměrné omezení

`MapAPI` Core a DigitalProcesses v této verzi deleguje na dosavadní Razor-enriched endpointy. Jsou schopné vracet JSON podle Accept, ale projekty mají stále UI/Razor reference. Úplně UI-free build a oddělení MapAPI od Razor závislostí není součástí této změny.

Stále je zachována zděděná hierarchie `CoreDbContext → DigitalProcessesDbContext → HostDbContext`.

## Ověření

Na tomto prostředí není nainstalováno .NET SDK; build a testy nebyly spuštěny. Předchozích 153 úspěšných testů se vztahuje k verzi 115. Pro ověření spusťte `dotnet test SignFakeArchitecture.sln` a následně test generované aplikace.
