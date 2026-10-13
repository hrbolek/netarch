# Přidávání aplikací bez zásahu do Core

Tento Host je **composition root**. Veškeré nové projekty, databázové modely, DTO, endpointy a registrace přibývají pod `src/Apps/`. Adresář `src/Core/` se nemění.

## Postup pro aplikaci Projects se dvěma modely

1. Založte `src/Apps/Projects/Projects.Models`, `Projects.Services`, `Projects.Module` (a případně `Projects.Resolvers` / `Projects.UI`). Závislost je vždy *Projects → Core*, nikoli opačně.
2. Definujte `ProjectModel` a `ProjectTaskModel`, bezpečnostní `rbacobject_id` a své Create/Update/Response DTO. Dodržte typové kontrakty `IEntityProjection<TDB,TDTO>`, `IEntityMapper<TDB,TDTO>` a vstupní DTO kontrakty, dle potřeby.
3. EF konfigurace definujte v `Projects.Module` (či `Projects.Models`) prostřednictvím `IEntityTypeConfiguration<ProjectModel>` a `IEntityTypeConfiguration<ProjectTaskModel>`. Zde určete tabulky, FK, indexy a pravidla mazání.
4. Vlastní modul implementuje `IModule.ConfigureDatabase`; Host jeho EF mapování použije automaticky. `CoreDbContext` ani `HostDbContext` se kvůli další doméně nemění.
5. V `Host.csproj` přidejte `ProjectReference` na nový aplikační modul. V `Program.cs` přidejte `.AddProjects()` a `.MapProjects()` ve stejném pořadí jako u DigitalProcesses. **`AddHostDatabase(host.Configuration)` ponechte až za registrací všech aplikačních modulů.**
6. `Projects` služby volají pouze vlastní/bezpečnostní služby, přes zavedené autorizované query. Zabezpečení řádků samotné nestačí pro přístup k navázaným modelům, proto ověřujte oprávnění pro každý zpřístupněný objekt.
7. Doplňte integrační test přes HTTP a test sdíleného scoped DbContext: `CoreDbContext` a `HostDbContext` musí ukazovat na tutéž instanci; aplikační model musí být součástí HostDbContext.Model. Otestujte společnou transakci i rollback při chybě.

## Zásadní pravidla

- `Core` neodkazuje na žádné `Apps`.
- Jediným vlastníkem sestavení EF modelu pro **tento spustitelný host** je `HostDbContext`; registrace z modulů přesměruje `AddHostDatabase` na jediný kontext.
- Jednotlivá aplikace nevytváří druhý produkční DbContext pro vlastní transakce.
- Pokud se změní seznam sestavených aplikací a jejich EF konfigurací, ověřte databázové migrace a kompatibilitu schématu. Registrace konfigurace sama o sobě nevytvoří migraci.
- Další samostatně spustitelný Host lze vytvořit pod `Apps/OtherHost` s jinou množinou modulů a vlastním composition root.
- `DigitalProcessesDbContext` zůstává pouze jako kompatibilní samostatný EF kontext, není registrován ve složeném Hostu. Mapování dodává `IModule.ConfigureDatabase`.

## Ověření

```bash
dotnet test SignFakeArchitecture.sln
```

**Poznámka:** Dokud není samostatně otestované skládání dvou aplikačních modulů, je tento mechanismus návrhem rozšíření, nikoli empiricky potvrzenou multi-app instalací.

## Prefix URI a provozní endpointy (119)

Host předává modulu jednoznačný prefix přes `MapModules(prefixes, mode)`. Doménové endpointy mají tvar `/{prefix}/{entity}/{operation}/{id}`. `HostManagementModule` vlastní `/management/info` a ve vývoji `/management/debug`. Kořen `/` mapuje pouze konkrétní UI Host. V API-only režimu ho nemusí mapovat vůbec.


## Stabilization 123

HostDbContext now inherits CoreDbContext only. Applications contribute schema via IModule.ConfigureDatabase; do not derive HostDbContext from an app context or redirect an app-specific DbContext service to HostDbContext. Use CoreDbContext.Set<TEntity>() in shared-context application services. Use `dotnet build src/Apps/Host/Host.csproj -p:ArchitectureApiOnly=true` to select API endpoint mapping at compile time. No `HOST_MODULE_MODE` setting is used. Note: transitive UI references remain in Core.Hosting, DigitalProcesses.Module and Base; build-time representation selection is not yet assembly-level API/UI separation.
