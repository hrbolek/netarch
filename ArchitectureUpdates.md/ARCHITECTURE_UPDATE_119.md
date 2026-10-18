# ARCHITECTURE_UPDATE_119 — Modulové prefixy a HostManagement

## Úpravy

- `IModule.MapAPI` a `MapUI` přijímají `string prefix`. První segment doménových URI volí Host; běžné cesty zůstávají `/{prefix}/{entity}/{operation}/{id}`.
- Host mapuje explicitní prefixy pro Core (`architecture`), DigitalProcesses (`digital-processes`) a HostManagement (`management`). Duplicitní nebo prázdné prefixy jsou odmítnuty.
- Nový volitelný `src/Apps/HostManagement` registruje `/management/info` a pouze ve vývojovém prostředí `/management/debug`. Diagnostický HTML výpis může obsahovat citlivé identity/hlavičky, nesmí být nasazen veřejně.
- `/` je nyní výhradně v Hostu: UI Host přesměruje na `/architecture`; API-only Host kořen vůbec nemusí mapovat.
- UI přehled `/architecture` zajišťuje Host, nikoli Core. Core mapuje entitní operace pod konfigurovaným prefixem.
- DigitalProcesses pomocné routy jsou registrovány relativně uvnitř `MapGroup(prefix)`, entitní resolvery dostávají prefix přímo. Stávající HTML obsah / odkazy mohou stále pracovat s původním `digital-processes` prefixem; kompletní přenastavení odkazů ve vykreslování je předmětem dalšího auditu.
- Aktualizován `ApplicationTemplate` a Host integrační testy pro `/management/info`, zrušení `/info`, a redirect `/`.

## Přidání dalšího modulu

Registrace DI: `.AddArchitectureModule<ProjectsModule>()`; v Hostu přidat `typeof(ProjectsModule) => "projects"` do prefixové mapy a referenci projektu. Žádné změny v Core.

## Testy

Spusťte `dotnet test SignFakeArchitecture.sln`. Runtime .NET SDK není v prostředí přípravy archivu dostupný, proto kompilaci a testy nemohu potvrdit. Je potřeba ověřit zejména zachování existujících endpointů DigitalProcesses.

## Poznámka

Režim `MapAPI` ještě skutečně neodpojuje Razor projektové závislosti Core a DigitalProcesses. Tato dříve identifikovaná hranice není součástí verze 119.
