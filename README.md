
## Current persistence and RBAC architecture

The runtime uses PostgreSQL with a shared CoreDbContext (or an application-derived
context). Entity services implement `ISecuredEntityService<TModel>` through
`SecuredEntityService<TModel>` in AuthorizationLayer. `IRbacAuthorizationService`
and `RbacAuthorizationService` decide actor/object/operation permissions;
`IAuthorizationLayer` applies those permissions to data access.
The redundant `IBaseService`, `ISecurityService` and HTTP `IRbacService` adapters
are removed. `SecurityPermissionService` remains the administration projection.
See [the secured service guide](SECURED_SERVICE_GUIDE.md) and
[the contract documentation](src/Core/Contracts/README.md#secured-entity-services-and-write-contracts).

# SignFakeArchitecture / WebAppArchitecture

Modulární ukázková architektura pro ASP.NET Core 8, Minimal API a Razor Components.

Projekt odděluje obecnou infrastrukturu od logiky jednotlivých entit. Každá entita je rozdělena do samostatných projektů `Models`, `Services`, `UI` a `Resolvers`. Opakující se infrastruktura je soustředěna v projektu `Base` a nové entity lze vytvořit pomocí `dotnet new` templates uložených v adresáři `templates/`.

Aktuální entita `User` slouží jako referenční implementace.

---

## 1. Struktura solution

```text
SignFakeArchitecture.sln
│
├── src/
│   ├── Apps/
│   │   ├── Host/
│   │   ├── AppComposition/
│   │   └── DigitalProcesses/
│   └── Core/
│       ├── Base/
│       │   ├── Base.csproj
│       │   └── UI/
│       ├── Contracts/
│       │   └── Core.Contracts.csproj
│       ├── AuthorizationLayer/
│       ├── DBLayer/
│       │   ├── Entities/
│       │   ├── Authorization/
│       │   └── Seeding/
│       ├── Hosting/
│       ├── Group/
│       ├── GroupType/
│       ├── Role/
│       ├── RoleType/
│       ├── Membership/
│       ├── Security/
│       └── User/
│
├── templates/
│   ├── EntityModelTemplate/
│   ├── EntityServiceTemplate/
│   ├── EntityUITemplate/
│   └── EntityResolversTemplate/
│
├── Directory.Build.props
├── Dockerfile
└── README.md
```

Build artefakty nejsou ukládány do jednotlivých projektů. `Directory.Build.props` zapíná centralizovaný artifacts layout:

```text
.build/
├── bin/
├── obj/
├── publish/
└── package/
```

Adresář se zdrojovými kódy tak zůstává bez lokálních `bin/` a `obj/` adresářů.

### Source ownership and naming

Folders describe ownership and responsibility; project names identify assemblies.
For example, `Core/Contracts/Core.Contracts.csproj` keeps the assembly name
`Core.Contracts` without repeating `Core` in the folder name.
Generic UI lives in `Core/Base/UI`; there is no second `Base.UI` source root.

Each Core entity owns its Models, Services and Resolvers projects. GroupType is
now a peer of Group, matching RoleType/Role: its DTOs, service and resolvers live
under `Core/GroupType`, not inside Group. No empty GroupType UI project or new
pages were introduced. Its existing five routes and JSON property names remain
unchanged; CoreModule now maps its endpoints explicitly.

Small single-entity Models/Services/Resolvers projects put their source files
next to the project file, rather than repeating the layer as an inner directory.
UI keeps meaningful `Pages`, `Components` and `Vectors` folders; large
multi-entity application projects keep entity-specific folders.
The `Entity*Template` scaffolds follow the same flat non-UI layout.

DBLayer remains one assembly and one shared Core context. Each built-in entity's
model, store interface and EF store are together in `DBLayer/Entities/{Entity}`.
RBAC projection/mapping models live in `DBLayer/Authorization`; initialization,
import and seed data live in `DBLayer/Seeding`. Shared database infrastructure
stays at the project root. These physical moves do not change database type
namespaces, EF mappings, tables or schema.

GroupType's moved public types now use `WebAppArchitecture.GroupType.Models`,
`.Services` and `.Resolvers` and their corresponding assemblies. External
consumers must update references/usings and rebuild; callers of
`MapGroupEndpoints` must also register `MapGroupTypeEndpoints`. Other physical
moves preserve namespaces and assembly names. No parallel definitions or
compatibility facade coupling Group back to GroupType were added.

`CoreLayoutTests` checks these source conventions and project-cycle freedom;
runtime ownership tests verify Group assemblies no longer define GroupType.
The GroupType HTTP regression checks exact route methods/paths, handlers,
list/read/link/update/delete and authorization denial. This uses existing xUnit
tooling, not a new architecture auditor.

Validation after the layout refactoring:

- Both root solution formats build in Release without errors; the full workflow
  build reports 9 existing compiler/analyzer warnings, the subsequent slnx build 4.
- All 317 tests pass: Core.Contracts 4, AuthorizationLayer 105, Host 18,
  DigitalProcesses 190. The new focused layout/ownership/GroupType checks pass 7/7.
- All 52 project graphs have valid reference targets and no cycles. The
  standalone inspector solution builds without errors or warnings.
- The direct/transitive NuGet audit covers 48 solution projects and reports no
  known vulnerabilities or audit problems. The validation workflow returns 0.
- Model/service/resolver scaffolds generate the expected flat layout in an
  isolated template hive. This is not a claim of full scaffold implementation
  or generated-domain compilation.

## Shared public contracts

[Core.Contracts](src/Core/Contracts/README.md) contains the existing public
entity, DTO and service interfaces under their original namespaces, plus minimal
vector value types. It has no project or NuGet dependencies. Base forwards its
old exported contract types to this assembly for compatibility.

The new `IAuthorizedQuery<TDBModel>` and `IAuthorizedQueries` are contracts only;
implementation and DI registration in AuthorizationLayer are a separate next step.
Existing services, RBAC, endpoint behavior and DTO mapping are unchanged.

## Authorization responsibilities

Security infrastructure is split by responsibility, not by similarly named
folders:

| Location | Responsibility |
| --- | --- |
| `src/Core/Contracts/Security` | Pure RBAC decision contract, operation names/IDs and well-known scopes; .NET BCL only |
| `src/Core/AuthorizationLayer` | Existing RBAC enforcement, authorized queries, controlled writes and effective-access projection |
| `src/Core/Security` | Application permission DTOs, administration services, resolvers and UI |
| `src/Core/Hosting/Identity` | HTTP actor implementation, trusted-header middleware and Entra endpoint integration |
| `src/Core/Base/Identity` | Only the existing HTTP-facing actor/authorization abstractions used by generic resolvers and UI |
| `src/Core/DBLayer/Seeding` | Existing fake-identity seed data consumed by DatabaseInitializer |

The former SecurityLayer project/namespace and ISecurityLayer/SecurityLayer
types have been renamed to AuthorizationLayer and
IAuthorizationLayer/AuthorizationLayer. Solutions, DI, consumers, tests and
templates use the new names. Existing authorization rules, identity-source
precedence, operations, database identifiers and active routes are unchanged.
The unused legacy Base RBAC endpoint branch and its exclusive request/record
types were removed; the active Security module remains.

`RbacOperations` is the canonical operation vocabulary; `SecurityOperations`
remains a set of compatibility aliases. Pure relocated RBAC types retain
their namespaces and are forwarded from Base. The renamed layer and relocated
HTTP implementations/seed types require external consumers to update project
references and rebuild; no binary facade creating reverse dependencies is added.
See [Core.Contracts](src/Core/Contracts/README.md#authorization-boundary-consolidation).
Authorized query implementations remain a separate step.

Validation of this consolidation: main Release build passed with 0 errors
and 26 existing warnings. The full validation workflow returned 0 with contracts
4/4, Host 14/14, DigitalProcesses 190/190 and authorization 104/104. After adding
the operation-alias regression, the complete authorization suite was rerun and
passed 105/105, giving 313 passing tests across the four suites. The retained
standalone inspector solution builds with 0 warnings/errors. All 49 project
graphs are acyclic with valid reference targets; the direct/transitive NuGet
audit reports no known vulnerabilities or audit problems across 45 solution
projects.

## Home and designer UI regression expectations

The Host root `/` renders application cards from the navigation registry (and
returns that registry's applications when JSON is requested); it no longer
redirects to `/architecture`.

The form designer uses the insertion wizard's Library in both whole-form and
subtree editors. The obsolete standalone Library card has been removed.
The AI designer operates on a whole form version and is shown only in its
whole-form draft editor, not in subtree, preview or published views.
Form and workflow permission panels are rendered once by the corresponding
version LargeCard, not duplicated inside the designer workspace.

Regression tests now check these intended behaviors rather than restoring the
old redirect, duplicate permission panel or standalone Library card. The
retirement/extraction validation results below describe their historical state
before this correction.

Validation after this correction (`./tools/audit-architecture.sh`): Release
build passed with 0 errors and 4 existing analyzer warnings; all 311 tests
passed (Core.Contracts 4, AuthorizationLayer 104, Host 13, DigitalProcesses 190).
The direct/transitive NuGet audit covered 45 projects with no known
vulnerabilities or audit problems. The validation script returned 0.

## Architecture annotations retired

Architecture annotations have been removed from application code and templates.
Other attributes, DTO names, existing generic model/DTO/service interfaces,
application logic and DI registrations are unchanged. Projects no longer inherit
an `Architecture.Attributes` reference or global using from
[Directory.Build.props](Directory.Build.props).

The attribute-driven auditor and its test project were removed at the user's
request, including their integrated contract/project-reference checks. No
replacement architecture auditor or equivalent static coverage is claimed.
Existing application tests and NuGet security checks remain active in
[CI](.github/workflows/architecture-audit.yml).

```bash
./tools/audit-architecture.sh
```

This compatibility entry point now builds/tests the application and audits
direct/transitive NuGet vulnerabilities into `artifacts/nuget-audit.json`.
It still runs the NuGet audit if tests fail, and reports a nonzero exit code for
build/test/audit-command failures. It no longer creates architecture findings;
old architecture reports are historical.

The unused `Architecture.Attributes` library is retained only for review in its
standalone solution, outside the application build and CI. Removing that library
and its standalone solution is the proposed next cleanup, subject to approval
for deleting their public types. See the
[retirement notes](tools/ArchitectureInspector/README.md).

Annotation retirement did not introduce authorized-query implementations or
change authorization behavior. The subsequent Core.Contracts extraction adds
only their public interfaces, as described above.

Validation after retirement:

- Removed 307 annotation declarations from 286 application/template files.
  A syntax-aware transformation checked that other C# tokens, attributes and
  generic contracts were preserved, including combined, multiline and
  fully-qualified attribute syntax. A second pass found no remaining annotations.
- The application Release build and the retained standalone library build pass.
  The application still reports existing compiler/analyzer warnings.
- Existing application suites: AuthorizationLayer 104/104 passed, Host 10/11 passed,
  DigitalProcesses 181/188 passed. The same 8 known failures concern root
  redirection and rendered UI text.
- The new `ArchitectureDependencyTests` regression passes separately and checks
  that the application runtime dependency graph contains neither retired tooling
  assembly. Evaluated production references/global imports are also clean.
- NuGet auditing remains enabled in `all` mode, with central transitive pinning
  enabled. The generated direct/transitive vulnerability report contains no
  known vulnerabilities. The validation script remains failing when tests fail;
  annotation removal does not hide those failures.

## NuGet Central Package Management

All projects under `src/`, `tests/`, and `tools/` inherit package versions from
the root [Directory.Packages.props](Directory.Packages.props). Keep
`PackageReference` items and their asset metadata in the project, but declare
versions only as central `PackageVersion` items. The standalone
[remaining attribute-library solution](tools/ArchitectureInspector/ArchitectureInspector.slnx)
uses the same file without depending on the application solution, but is not
part of the active workflow.

Central transitive pinning remains enabled. The cryptography pins apply even
when these packages are only transitive dependencies. NuGet security auditing
is not disabled or suppressed.

| Package | Central version |
| --- | --- |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.13 |
| Microsoft.EntityFrameworkCore | 10.0.13 |
| Microsoft.EntityFrameworkCore.Relational (transitive pin) | 10.0.13 |
| Microsoft.Identity.Web | 3.14.1 |
| Microsoft.NET.Test.Sdk | 17.11.1 |
| Npgsql | 10.0.3 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
| Swashbuckle.AspNetCore | 6.6.2 |
| System.Security.Cryptography.Pkcs | 9.0.21 |
| System.Security.Cryptography.Xml (transitive pin) | 9.0.21 |
| xunit | 2.9.2 |
| xunit.runner.visualstudio | 2.8.2 |

No conflicting direct package versions or project-specific version exceptions
were needed. The follow-up .NET 10 compatibility repair upgrades EF Core from
8.0.10 to 10.0.13 and Npgsql/provider to 10.0.3. The Relational pin keeps EF Core
runtime packages on the same patch level. This fixes LINQ parameter evaluation
involving `ReadOnlySpan<Guid>` on .NET 10. ASP.NET Core's test host is upgraded
with `Microsoft.AspNetCore.Mvc.Testing` to avoid the old response pipe writer's
missing `UnflushedBytes` implementation.

The retired auditor's Roslyn and MSBuild locator package versions have been
removed from CPM because no remaining project consumes them. Unrelated transitive
dependencies may resolve differently between the standalone tools and the
application; CPM does not force every transitive dependency to one version.

Reproduce validation from the repository root with .NET 10:

```bash
dotnet restore SignFakeArchitecture.sln
dotnet build SignFakeArchitecture.sln -c Release --no-restore
dotnet build tools/SqliteDumpToPostgres/SqliteDumpToPostgres.csproj -c Release
dotnet test SignFakeArchitecture.sln -c Release --no-build --no-restore

# Full direct/transitive graph; repeat with --vulnerable for the security audit.
dotnet list SignFakeArchitecture.sln package --include-transitive --no-restore
dotnet list tools/SqliteDumpToPostgres/SqliteDumpToPostgres.csproj package --include-transitive --no-restore
```

Historical migration/compatibility validation before annotation retirement
(.NET SDK 10.0.401; auditor results below no longer describe active checks):

- All 49 scoped projects restored with CPM; all 26 project package references
  are versionless and retain their other metadata.
- Both solutions and the standalone PostgreSQL utility built successfully.
  The application build reported compiler/analyzer warnings; the inspector
  and utility builds had no warnings.
- NuGet audits with `--vulnerable --include-transitive` reported no known
  vulnerabilities for either solution or the utility. Current project assets
  resolve Xml/Pkcs exclusively to 9.0.21.
- Application tests: AuthorizationLayer 104 passed / 0 failed, Host 10 passed /
  1 failed, DigitalProcesses 181 passed / 7 failed. The remaining failures
  concern expected root redirection and rendered UI text, not compilation,
  LINQ evaluation or response pipe writers. No application behavior or test
  expectations were changed to hide these failures.
- Inspector tests: 27 passed / 0 failed, including a standalone Razor workspace
  regression and accepted/rejected layer/role pairs. `ARCH003` now validates
  the actual supported `Model`, `Resolver` and `Service` enum names rather than
  nonexistent `API`/`Services` layers.
- The architecture audit analyzes all 40 selected production projects with
  no compilation failures or workspace diagnostics (3 test projects skipped).
  It still exits 1 for 65 architecture violations: 58 `ARCH002`, 1 `ARCH003`,
  and 6 `ARCH004`. These are not compiler errors. The report is written to
  `artifacts/architecture-audit.json`; existing application classifications and
  generic contracts were not changed in the compilation repair.

---

# 2. Architektura

Základní tok závislostí je:

```text
                         ┌───────────────┐
                         │     Host      │
                         └───────┬───────┘
                                 │
                                 ▼
                         ┌───────────────┐
                         │   Resolvers   │
                         └────┬─────┬────┘
                              │     │
                    ┌─────────┘     └─────────┐
                    ▼                         ▼
              ┌──────────┐               ┌──────────┐
              │ Services │               │    UI    │
              └────┬─────┘               └────┬─────┘
                   │                          │
                   └──────────┬───────────────┘
                              ▼
                         ┌──────────┐
                         │  Models  │
                         └────┬─────┘
                              │
                              ▼
                         ┌──────────┐
                         │   Base   │
                         └──────────┘
```

Prakticky:

```text
Base
  ↑
Models
  ↑       ↑
Services  UI
   \      /
    Resolvers
       ↑
      Host
```

## Base

`src/Base` obsahuje znovupoužitelnou infrastrukturu společnou pro všechny entity, zejména:

- základní datové kontrakty (`IEntityDBModel`, DTO kontrakty),
- `IBaseStore<T>` a základní/in-memory store,
- servisní kontrakt `ISecuredEntityService<TDBModel>` v Core.Contracts,
- generické CRUD resolver metody v `BaseResolvers`,
- Hasura-like filtrování přes `HasuraFilterExpression`,
- endpoint helpers `ApplyJsonModel` a `ApplyViewModel`,
- společné Razor komponenty,
- společné Razor stránky.

Projekt `Base` neobsahuje logiku konkrétní entity.

## Models

Například `User.Models` obsahuje:

- `UserModel` – persistence/store model,
- `UserResponse` – DTO vracené z service/resolver vrstvy,
- `UserCreateRequest` – payload pro create,
- `UserUpdateRequest` – payload pro update,
- `IUserStore`,
- `InMemoryUserStore` pro demo provoz.

Typicky:

```text
HTTP Create payload
      │
      ▼
UserCreateRequest
      │ ToModel()
      ▼
UserModel
      │
      ▼
Store
```

Response směr je opačný:

```text
Store
  │
  ▼
UserModel
  │ UserResponse.FromModel(...)
  ▼
UserResponse
  │
  ▼
Resolver / JSON / Razor
```

## Services

Service vrstva reprezentuje aplikační/business logiku.

Konkrétní služba typicky pouze fixuje obecné typy:

```csharp
public interface IUserService : ISecuredEntityService<UserModel>
{
}

public sealed class UserService
    : SecuredEntityService<UserModel>, IUserService
{
    public UserService(IAuthorizationLayer authorization, IDBLayer db) : base(authorization, db)
    {
    }
}
```

Standardní zabezpečené operace poskytuje `SecuredEntityService`. Konkrétní service
přidává doménové invarianty a synchronizaci projekcí; ukázka vynechává jejich
závislosti. Vytváření zůstává specializovanou doménovou operací, ne obecným CRUD.

Aktualizace používá samostatný request a výstupní DTO:

```csharp
UpdateAsync<TUpdateRequest, TDTO>(...)
```

Request implementuje `IUpdateDTOModel<TModel>` a obsahuje povinný původní
`updated_at` jako concurrency token. Auditní hodnoty vytváří server; request je
nesmí přepisovat. Chybějící/neplatný token vrací 400, souběžná změna 409.

## UI

UI projekt obsahuje specializované Razor komponenty a stránky dané entity.

Pro `User` například:

```text
User.UI/
├── Components/
│   ├── UserMediumCard.razor
│   ├── UserLargeCard.razor
│   └── UserEditableCard.razor
└── Pages/
    ├── UserViewPage.razor
    ├── UserEditPage.razor
    └── UserViewListPage.razor
```

Specializované komponenty entity skládají obecné komponenty z `Base.UI`.

Doporučené pojmenování je explicitní (`UserMediumCard`, `DocumentMediumCard`, ...), aby nedocházelo ke kolizím s generickými komponentami `MediumCard<TModel>`, `LargeCard<TModel>` apod.

## Resolvers

Resolver představuje HTTP/API vrstvu.

Konkrétní resolver zachovává konkrétní signaturu endpointu kvůli:

- ASP.NET Core Minimal API bindingu,
- dependency injection,
- Swagger/OpenAPI,
- konkrétním request/response typům.

Samotnou opakující se CRUD mechaniku deleguje do `BaseResolvers`.

Příklad:

```csharp
public static async Task<IResult> Create(
    UserCreateRequest request,
    IUserService service,
    CancellationToken cancellationToken)
{
    return await BaseResolvers.Create<
        UserModel,
        UserResponse,
        UserCreateRequest,
        IUserService>(
            request,
            service,
            cancellationToken);
}
```

Tím zůstává HTTP kontrakt explicitní, ale implementace společných operací není kopírována mezi entitami.

## Host

`Host` je executable ASP.NET Core projekt.

Je odpovědný zejména za:

- Dependency Injection,
- Swagger,
- registraci služeb a storů,
- registraci endpointů jednotlivých modulů,
- spuštění webové aplikace.

Například:

```csharp
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddScoped<IUserService, UserService>();

...

app.MapUserEndpoints("/architecture");
```

---

# 3. Zprovoznění v Dev Containeru

## Požadavky na hostitelském počítači

Doporučeno:

- Docker Desktop nebo jiný Docker runtime,
- Visual Studio Code,
- extension **Dev Containers**.

.NET SDK nemusí být instalováno přímo na hostitelském počítači, pokud je vývoj prováděn uvnitř devcontaineru.

## Otevření projektu

1. Naklonujte nebo otevřete repository ve VS Code.
2. Otevřete Command Palette (`Ctrl+Shift+P`).
3. Vyberte:

```text
Dev Containers: Reopen in Container
```

Po otevření terminálu ověřte prostředí:

```bash
dotnet --info
```

Projekt používá .NET 8.

## Spuštění za fake Easy Entra ID

Pro lokální běh za reverzní proxy, která simuluje easy_entra_id, použijte:

```bash
docker compose up --build
```

Veřejný vstup je potom na:

```text
http://localhost:8000
```

Login obrazovku poskytuje kontejner `fake-entra`, který do požadavků vůči aplikaci přidává hlavičky `x-ms-client-principal`, `x-ms-client-principal-id` a `x-ms-client-principal-name`.

Aplikace akceptuje dostupné EasyAuth hlavičky flexibilně: pokud není k dispozici kompletní `x-ms-client-principal`, použije fallback z `x-ms-client-principal-id` a `x-ms-client-principal-name`.

Aplikace při startu seeduje odpovídající uživatele a RBAC oprávnění pro tyto fake identity:

- John Newbie
- Planner Admin
- Auditor

Soubor `docker/fake-users.json` obsahuje pro uživatele i `scopes` (např. `read:profile`, `read:messages`) a tyto scope se mapují do claimů `scp` a `scope`.

## Restore a build

Z rootu repository:

```bash
dotnet restore SignFakeArchitecture.sln
dotnet build SignFakeArchitecture.sln
```

Případně:

```bash
dotnet build
```

pokud se nacházíte v rootu solution a CLI solution jednoznačně rozpozná.

## Spuštění Hostu

```bash
dotnet run --project src/Apps/Host/Host.csproj
```

Host naslouchá na:

```text
http://localhost:8080
```

Swagger UI:

```text
http://localhost:8080/swagger
```

Pokud používáte VS Code Dev Containers, VS Code může port `8080` automaticky forwardovat na hostitelský počítač. Forwarding lze zkontrolovat v panelu **Ports**.

## Clean build

Projekt používá centralizovaný `.build/` adresář. Pro úplné vyčištění build artefaktů:

```bash
rm -rf .build

dotnet restore SignFakeArchitecture.sln
dotnet build SignFakeArchitecture.sln
```

---

# 4. Instalace templates

Templates jsou lokální `dotnet new` šablony uložené přímo v repository.

V devcontaineru je instalujte z rootu repository:

```bash
dotnet new install ./templates/EntityModelTemplate
dotnet new install ./templates/EntityServiceTemplate
dotnet new install ./templates/EntityUITemplate
dotnet new install ./templates/EntityResolversTemplate
```

Dostupnost ověřte:

```bash
dotnet new list
```

Měly by být dostupné short names:

```text
entity-model
entity-service
entity-ui
entity-resolvers
```

Nápověda konkrétní šablony:

```bash
dotnet new entity-model --help
dotnet new entity-service --help
dotnet new entity-ui --help
dotnet new entity-resolvers --help
```

> Instalace templates je lokální pro dané prostředí. Po vytvoření nového devcontaineru může být potřeba templates znovu nainstalovat. Je vhodné tuto instalaci případně přidat do `postCreateCommand` devcontaineru.

---

# 5. Vytvoření nové entity – doporučený algoritmus

Následující postup používá jako příklad novou entitu `Document`.

Předpokládané namespace:

```text
WebAppArchitecture.Document
```

a zobrazovaný název:

```text
Dokument
```

## Krok 1 – vygenerovat Models

```bash
dotnet new entity-model \
    -n Document \
    -o src/Document.Models \
    --rootNamespace WebAppArchitecture
```

Vznikne projekt:

```text
src/Document.Models/
```

Následně upravte vygenerovaný model podle skutečné domény entity.

Typicky zkontrolujte/upravte:

- vlastnosti `DocumentModel`,
- `DocumentResponse`,
- `DocumentCreateRequest`,
- `DocumentUpdateRequest`,
- mapování `FromModel`, `ToModel`, `ApplyTo`,
- store interface,
- konkrétní store implementaci.

Template obsahuje pouze jednoduchý výchozí model (`id`, `Name`).

## Krok 2 – vygenerovat Services

```bash
dotnet new entity-service \
    -n Document \
    -o src/Document.Services \
    --rootNamespace WebAppArchitecture
```

Výchozí `DocumentService` zdědí standardní CRUD chování z `BaseService`.

Pokud entita potřebuje další business operace, přidejte je do:

```text
IDocumentService
DocumentService
```

Například:

```csharp
Task<DocumentResponse> SignAsync(...);
```

nebo jinou entity-specific operaci.

## Krok 3 – vygenerovat UI

```bash
dotnet new entity-ui \
    -n Document \
    -o src/Document.UI \
    --rootNamespace WebAppArchitecture \
    --title "Dokument"
```

Poté upravte entity-specific komponenty podle vlastností DTO:

```text
DocumentMediumCard.razor
DocumentLargeCard.razor
DocumentEditableCard.razor
```

a případně stránky:

```text
DocumentViewPage.razor
DocumentEditPage.razor
DocumentViewListPage.razor
```

## Krok 4 – vygenerovat Resolvers

```bash
dotnet new entity-resolvers \
    -n Document \
    -o src/Document.Resolvers \
    --rootNamespace WebAppArchitecture \
    --route document
```

Resolver vrstva vytvoří standardní endpointy a deleguje CRUD operace do `BaseResolvers`.

Po vygenerování zkontrolujte zejména:

- konkrétní Create request typ,
- konkrétní Update request typ,
- route group,
- response modely,
- `ApplyJsonModel` / `ApplyViewModel`,
- případné vlastní resolver metody.

## Krok 5 – přidat projekty do solution

```bash
dotnet sln SignFakeArchitecture.sln add src/Document.Models/Document.Models.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.Services/Document.Services.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.UI/Document.UI.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.Resolvers/Document.Resolvers.csproj
```

Ověření:

```bash
dotnet sln SignFakeArchitecture.sln list
```

Poznámka: `ProjectReference` určuje skutečné compile-time závislosti mezi projekty. Přidání projektu do `.sln` slouží primárně k organizaci a společnému buildu solution.

## Krok 6 – zkontrolovat ProjectReference

Očekávané závislosti jsou:

```text
Document.Models
    └── Base

Document.Services
    ├── Base
    └── Document.Models

Document.UI
    ├── Base
    └── Document.Models

Document.Resolvers
    ├── Base
    ├── Document.Models
    ├── Document.Services
    └── Document.UI
```

Tyto reference templates generují automaticky, přesto je vhodné je po scaffoldingu zkontrolovat.

## Krok 7 – registrovat Store a Service v Hostu

Do `src/Apps/Host/Program.cs` přidejte namespace:

```csharp
using WebAppArchitecture.Document.Models;
using WebAppArchitecture.Document.Services;
using WebAppArchitecture.Document.Resolvers;
```

Pro demo in-memory variantu:

```csharp
builder.Services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
```

`Singleton` store zajistí, že in-memory data přežijí po celou dobu běhu procesu Hostu.

## Krok 8 – zaregistrovat endpointy

V `Program.cs`:

```csharp
app.MapDocumentEndpoints("/architecture");
```

Konkrétní výsledná URL závisí na route group definované resolver template.

## Krok 9 – build

```bash
dotnet build SignFakeArchitecture.sln
```

Pokud build selže po změnách generických Razor komponent, může pomoci úplné odstranění generovaných artefaktů:

```bash
rm -rf .build
dotnet restore SignFakeArchitecture.sln
dotnet build SignFakeArchitecture.sln
```

## Krok 10 – spustit a ověřit Swagger

```bash
dotnet run --project src/Apps/Host/Host.csproj
```

Otevřete:

```text
http://localhost:8080/swagger
```

Ověřte minimálně:

- Create,
- Read,
- List,
- Vector,
- Update,
- Delete,
- View/Edit/List Razor varianty, pokud jsou pro entitu použity.

---

# 6. Standardní CRUD tok

## Read

```text
HTTP GET
   │
   ▼
EntityResolvers.Read
   │
   ▼
BaseResolvers.Read
   │
   ▼
IEntityService.GetByIdAsync
   │
   ▼
Store.GetByIdAsync
   │
   ▼
DB model
   │ FromModel()
   ▼
Response DTO
```

## Create

```text
CreateRequest
     │
     ▼
EntityResolvers.Create
     │
     ▼
BaseResolvers.Create<...>
     │
     ▼
BaseService.CreateAsync<TCreateRequest>
     │
     │ request.ToModel()
     ▼
DB model
     │
     ▼
Store.CreateAsync
     │
     ▼
Response DTO
```

Create payload tedy nemusí být stejný jako Response DTO.

## Update

```text
UpdateRequest
     │
     ▼
EntityResolvers.Update
     │
     ▼
BaseResolvers.Update<...>
     │
     ▼
BaseService.UpdateAsync<TUpdateRequest>
     │
     ├── Store.GetByIdAsync(id)
     │
     ├── request.ApplyTo(current)
     │
     └── Store.UpdateAsync(updated)
              │
              ▼
        Response DTO
```

To umožňuje update payloadu obsahovat pouze data, která mají být změněna, zatímco persistence model může obsahovat další interní atributy.

---

# 7. List a Vector

Service vrstva vrací obecnou read-only kolekci DTO.

Resolver na HTTP hranici převádí kolekci na pole:

```csharp
return ApiResults.Ok(result.ToArray());
```

To je důležité, pokud endpoint deklaruje:

```csharp
.ApplyJsonModel<EntityResponse[]>()
```

nebo:

```csharp
.ApplyViewModel<EntityViewListPage, EntityResponse[]>()
```

Runtime typ musí odpovídat typu deklarovanému endpoint helperem.

---

# 8. Hasura-like filtrování

Pro obecné filtrování kolekcí je publikována operace `Vector`.

Resolver přijme JSON dictionary a převede ji pomocí:

```csharp
HasuraFilterExpression.Build<TEntityModel>(request.Where)
```

na:

```csharp
Expression<Func<TEntityModel, bool>>
```

Service/store vrstva tedy nezná transportní JSON formát.

Příklad payloadu:

```json
{
  "where": {
    "Name": { "_ilike": "%demo%" },
    "_or": [
      { "Name": { "_eq": "Alpha" } },
      { "Name": { "_eq": "Beta" } }
    ]
  }
}
```

Toto rozdělení umožňuje stejný service kontrakt použít jak nad `InMemoryStore`, tak později nad databázovým store.

---

# 9. Endpoint model – JSON a Razor

Projekt používá endpoint helpers:

```csharp
.ApplyJsonModel<T>()
```

a:

```csharp
.ApplyViewModel<TPage,TModel>()
```

Stejný resolver tak může vrátit DTO, které se podle endpointu interpretuje buď jako JSON, nebo jako model Razor stránky.

Například Read resolver lze vystavit dvakrát:

```csharp
group.MapGet("/view/{id:guid}", EntityResolvers.Read)
    .ApplyViewModel<EntityViewPage, EntityResponse>();

group.MapGet("/edit/{id:guid}", EntityResolvers.Read)
    .ApplyViewModel<EntityEditPage, EntityResponse>();
```

Resolver zůstává stejný; mění se pouze způsob prezentace výsledku.

---

# 10. Dependency Injection a životnost storů

Pro demo in-memory store je vhodné:

```csharp
builder.Services.AddSingleton<IEntityStore, InMemoryEntityStore>();
```

Data potom existují po celou dobu běhu Host procesu.

Service může být například scoped:

```csharp
builder.Services.AddScoped<IEntityService, EntityService>();
```

Při nahrazení in-memory store databázovým store se resolver ani standardní service kontrakt nemusí měnit.

---

# 11. Přidávání specifických operací entity

Generické `BaseService` a `BaseResolvers` řeší pouze společnou mechaniku.

Pokud má entita business operaci mimo CRUD, přidejte ji explicitně.

Například:

```csharp
public interface IDocumentService
    : ISecuredEntityService<DocumentModel>
{
    Task<DocumentResponse> SignAsync(
        Guid id,
        DocumentSignRequest request,
        CancellationToken cancellationToken = default);
}
```

A odpovídající resolver:

```csharp
public static async Task<IResult> Sign(
    Guid id,
    DocumentSignRequest request,
    IDocumentService service,
    CancellationToken cancellationToken)
{
    var result = await service.SignAsync(id, request, cancellationToken);
    return ApiResults.Ok(result);
}
```

Tím je zachováno pravidlo:

```text
Base = společná mechanika
Entity service/resolver = konkrétní význam operace
```

---

# 12. Templates – princip fungování

Každá template má `.template.config/template.json`.

Důležité vlastnosti:

```json
"sourceName": "EntityName"
```

znamená, že při:

```bash
dotnet new entity-model -n Document ...
```

se výskyty `EntityName` nahrazují `Document`, a to v názvech souborů i v jejich obsahu.

Parametr:

```bash
--rootNamespace WebAppArchitecture
```

nahrazuje placeholder:

```text
RootNamespace
```

UI template navíc podporuje:

```bash
--title "Dokument"
```

který nahrazuje:

```text
EntityTitle
```

Resolver template podporuje také:

```bash
--route document
```

pro route-specific placeholder definovaný v template konfiguraci.

---

# 13. Vývoj templates

Po změně template ji může být potřeba v aktuálním prostředí znovu nainstalovat.

Nejprve zjistěte instalované templates:

```bash
dotnet new uninstall
```

Poté příslušnou lokální template znovu nainstalujte, například:

```bash
dotnet new install ./templates/EntityModelTemplate --force
```

nebo znovu nainstalujte všechny čtyři templates.

Před použitím template pro produkční entitu je vhodné udělat testovací scaffold do dočasného adresáře a ověřit build.

Například:

```bash
rm -rf /tmp/TestEntity.*

dotnet new entity-model -n TestEntity -o /tmp/TestEntity.Models --rootNamespace WebAppArchitecture
```

---

# 14. Doporučený checklist nové entity

Po vygenerování nové entity ověřte:

- [ ] `*.Models.csproj` existuje a referencuje `Base`.
- [ ] persistence model obsahuje správná pole.
- [ ] Response DTO mapuje správně z DB modelu.
- [ ] Create request generuje správný DB model.
- [ ] Update request zachovává identitu entity a správně implementuje `ApplyTo`.
- [ ] Store interface odpovídá DB modelu.
- [ ] `*.Services.csproj` referencuje `Base` a `Models`.
- [ ] service dědí z odpovídajícího `BaseService`.
- [ ] UI komponenty používají entity-specific názvy (`EntityNameMediumCard`, ...).
- [ ] UI používá správný Response DTO.
- [ ] resolver používá správné Create/Update request typy.
- [ ] List/Vector vracejí runtime typ odpovídající `ApplyJsonModel` / `ApplyViewModel`.
- [ ] route parametry uvedené v URI existují také v signatuře handleru.
- [ ] všechny čtyři projekty jsou přidány do `.sln`.
- [ ] `Host` obsahuje potřebné `using`.
- [ ] Store je zaregistrován v DI.
- [ ] Service je zaregistrována v DI.
- [ ] endpoint extension je zaregistrována v `Host`.
- [ ] každý endpoint má při zapnuté Entra validaci právě jednu deklaraci `WithEntra(...)` nebo `WithoutEntra()`.
- [ ] chráněné endpointy používají správné Entra role.
- [ ] `Host` při použití Entra ID registruje `AddEntraEndpointSecurity`, `UseAuthentication`, `UseAuthorization` a `ValidateEntraEndpointSecurity`.
- [ ] `dotnet build SignFakeArchitecture.sln` proběhne bez chyby.
- [ ] Swagger ukazuje očekávané endpointy a request/response schemas.

---

# 15. Microsoft Entra ID – autentizace a autorizace endpointů

Projekt obsahuje v `Base` společnou podporu pro zabezpečení Minimal API endpointů pomocí Microsoft Entra ID.

Implementace je soustředěna v:

```text
src/Core/Hosting/Identity/EntraEndpointSecurity.cs
```

a poskytuje čtyři základní extension body:

```csharp
AddEntraEndpointSecurity(...)
WithEntra(...)
WithoutEntra()
ValidateEntraEndpointSecurity()
```

Cílem je, aby zabezpečení endpointů bylo **explicitní a kontrolovatelné přímo při startu aplikace**.

## Registrace Entra ID v Hostu

Do `Program.cs` přidejte:

```csharp
using WebAppArchitecture.Base.Security;
```

Před `builder.Build()` zaregistrujte autentizaci a autorizaci:

```csharp
builder.Services.AddEntraEndpointSecurity(
    builder.Configuration
);
```

Výchozí konfigurační sekce je:

```text
AzureAd
```

Lze použít i jiný název:

```csharp
builder.Services.AddEntraEndpointSecurity(
    builder.Configuration,
    "MyAzureAd"
);
```

Extension interně registruje:

```csharp
AddAuthentication(...)
AddMicrosoftIdentityWebApi(...)
AddAuthorization()
```

Po vytvoření aplikace je potřeba zapnout authentication a authorization middleware:

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

Typická struktura `Program.cs` tedy může být:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorComponents();

builder.Services.AddEntraEndpointSecurity(
    builder.Configuration
);

builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

...
```

## Konfigurace `AzureAd`

Konfigurace se načítá standardním způsobem přes `IConfiguration`, tedy například z:

```text
appsettings.json
appsettings.Development.json
environment variables
user secrets
secret store / deployment configuration
```

Příklad základní konfigurace:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<application-client-id>"
  }
}
```

Konkrétní hodnoty musí odpovídat registraci API aplikace v Microsoft Entra ID.

Citlivé hodnoty neukládejte do repository. Pro development používejte například environment variables nebo user secrets.

## Zabezpečený endpoint – `WithEntra`

Endpoint, který vyžaduje přihlášeného uživatele s konkrétní rolí, označte:

```csharp
group.MapPost("/create", EntityNameResolvers.Create)
    .WithName("EntityNameCreate")
    .WithEntra("Administrator")
    .ApplyJsonModel<EntityNameResponse>();
```

Více rolí:

```csharp
.WithEntra("Signer", "Administrator")
```

má v aktuální implementaci význam:

```text
Signer OR Administrator
```

Uživatel tedy musí být autentizovaný a musí mít alespoň jednu z uvedených rolí.

Role jsou normalizovány:

- prázdné hodnoty jsou odstraněny,
- mezery jsou ořezány,
- duplicity jsou odstraněny bez ohledu na velikost písmen.

Volání bez role není povoleno:

```csharp
.WithEntra()
```

a skončí chybou.

## Explicitně veřejný endpoint – `WithoutEntra`

Veřejný endpoint označte explicitně:

```csharp
app.MapGet("/", () => Results.Ok(new
{
    message = "App běží"
}))
.WithoutEntra();
```

nebo:

```csharp
group.MapGet("/public-info", EntityNameResolvers.PublicInfo)
    .WithoutEntra();
```

`WithoutEntra()` interně používá:

```csharp
AllowAnonymous()
```

ale současně přidává vlastní metadata, podle kterých lze později ověřit, že endpoint byl záměrně označen jako veřejný.

## Povinná explicitní deklarace zabezpečení

Důležitou vlastností architektury je metoda:

```csharp
app.ValidateEntraEndpointSecurity();
```

Ta projde všechny `RouteEndpoint` objekty a kontroluje, že každý endpoint obsahuje právě jednu bezpečnostní deklaraci:

```text
WithEntra(...)
```

nebo:

```text
WithoutEntra()
```

Pokud endpoint nemá ani jednu deklaraci, aplikace skončí s chybou typu:

```text
Endpoint '...' is missing security declaration.
Use WithEntra(...) or WithoutEntra().
```

Stejně tak je chyba, pokud má endpoint deklarace dvě.

To chrání aplikaci proti situaci, kdy vývojář vytvoří nový endpoint a omylem zapomene rozhodnout, zda má být veřejný nebo chráněný.

Validaci volejte až po namapování endpointů a před `Run()`:

```csharp
app.MapGet("/", ...)
    .WithoutEntra();

app.MapUserEndpoints("/architecture");

app.ValidateEntraEndpointSecurity();

app.Run(
    "http://0.0.0.0:8080"
);
```

## Entra ID v entity endpoint extensions

Bezpečnost lze deklarovat přímo v resolver template.

Například:

```csharp
group.MapGet("/view/{id:guid}", EntityNameResolvers.Read)
    .WithName("EntityNameRead")
    .WithEntra("Reader", "Administrator")
    .ApplyViewModel<EntityNameViewPage, EntityNameResponse>();

group.MapPost("/create", EntityNameResolvers.Create)
    .WithName("EntityNameCreate")
    .WithEntra("Editor", "Administrator")
    .ApplyJsonModel<EntityNameResponse>();

group.MapPut("/update", EntityNameResolvers.Update)
    .WithName("EntityNameUpdate")
    .WithEntra("Editor", "Administrator")
    .ApplyJsonModel<EntityNameResponse>();

group.MapDelete("/delete/{id:guid}", EntityNameResolvers.Delete)
    .WithName("EntityNameDelete")
    .WithEntra("Administrator");

group.MapGet("/list", EntityNameResolvers.List)
    .WithName("EntityNameList")
    .WithoutEntra()
    .ApplyViewModel<EntityNameViewListPage, EntityNameResponse[]>();
```

Zda mají být role součástí obecné template, je architektonické rozhodnutí.

Pro univerzální template je obvykle bezpečnější:

1. vygenerovat endpointy bez předpokladu konkrétních business rolí,
2. při implementaci entity explicitně doplnit `WithEntra(...)` nebo `WithoutEntra()`,
3. nechat `ValidateEntraEndpointSecurity()` odhalit endpointy, u kterých toto rozhodnutí chybí.

Tím template nevnáší do doménové entity role, které pro ni nemusí být správné.

## Doporučené pořadí v `Program.cs`

Kompletní základní pořadí:

```csharp
var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorComponents();

builder.Services.AddEntraEndpointSecurity(
    builder.Configuration
);

// entity DI
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// endpoints
app.MapGet("/", () => Results.Ok(new
{
    message = "App běží"
}))
.WithoutEntra();

app.MapUserEndpoints("/architecture");

// security sanity check
app.ValidateEntraEndpointSecurity();

// start
app.Run(
    "http://0.0.0.0:8080"
);
```

## Co Entra extension řeší a co ne

Tato vrstva řeší především:

```text
HTTP endpoint
    │
    ├── je uživatel autentizovaný?
    │
    └── má některou z požadovaných Entra rolí?
```

Je vhodná pro ochranu API hranice.

Nenahrazuje však jemnější entity-level autorizaci v business/service vrstvě, například rozhodování podle:

```text
EffectiveUserId
RbacObjectId
role konkrétního objektu
stav workflow
vlastnictví entity
```

Pokud aplikace používá i objektové RBAC, doporučené rozdělení je:

```text
Entra ID
   │
   │ autentizace + hrubá endpoint role
   ▼
Resolver
   │
   ▼
Service
   │
   │ entity-level / object-level authorization
   ▼
Store
```

Tím zůstává Entra ID odpovědné za identitu a coarse-grained API authorization, zatímco service vrstva může realizovat detailní business oprávnění.

> V aktuálním referenčním `Host/Program.cs` není Entra ID ještě aktivováno. Extension infrastruktura je připravena v `Base`; k aktivaci je potřeba přidat registraci služby, middleware, bezpečnostní deklarace endpointů a závěrečné `ValidateEntraEndpointSecurity()`.

---

# 16. Docker image

Repository obsahuje také `Dockerfile` pro produkční build Hostu.

Build:

```bash
docker build -t signfakearchitecture .
```

Spuštění:

```bash
docker run --rm -p 8080:8080 signfakearchitecture
```

Swagger:

```text
http://localhost:8080/swagger
```

Při přidání nových projektů do solution je nutné pamatovat také na Docker build cache sekci, která kopíruje `.csproj` soubory před `dotnet restore`. Nové projektové soubory je proto vhodné přidat do odpovídající části `Dockerfile`.

---

# 16. Základní návrhové principy

Architektura se řídí několika jednoduchými pravidly:

1. **Persistence model není HTTP response model.**  
   DB/store model a DTO mají oddělené role.

2. **Create a Update payload nemusí být stejný jako Response DTO.**  
   Každá operace může mít vlastní kontrakt.

3. **Resolver neřeší persistence logiku.**  
   Resolver se stará o HTTP kontrakt a deleguje do service.

4. **Service nezná Razor ani HTTP.**  
   Service pracuje s doménovými/persistence typy a DTO kontrakty.

5. **Transportní filtrovací syntaxe zůstává v resolver vrstvě.**  
   Service dostává typovaný expression predicate.

6. **Base vrstva obsahuje mechaniku, konkrétní modul význam.**

7. **Konkrétní endpoint signatures zůstávají explicitní.**  
   Je to důležité pro Minimal API binding, DI a Swagger.

8. **ProjectReference a `.sln` nejsou totéž.**  
   `ProjectReference` určuje build závislost; solution organizuje projekty a společný build.

9. **In-memory store je demo implementace.**  
   Později lze nahradit databázovým store bez změny HTTP kontraktu.

10. **Templates jsou výchozí scaffold, nikoli náhrada doménového návrhu.**  
    Po vygenerování je vždy nutné upravit vlastnosti, DTO, UI a případné business operace podle konkrétní entity.

---

# 17. Rychlá reference příkazů

## Build

```bash
dotnet restore SignFakeArchitecture.sln
dotnet build SignFakeArchitecture.sln
```

## Run

```bash
dotnet run --project src/Apps/Host/Host.csproj
```

## Templates

```bash
dotnet new install ./templates/EntityModelTemplate
dotnet new install ./templates/EntityServiceTemplate
dotnet new install ./templates/EntityUITemplate
dotnet new install ./templates/EntityResolversTemplate
```

## Nová entita `Document`

```bash
dotnet new entity-model -n Document -o src/Document.Models --rootNamespace WebAppArchitecture

dotnet new entity-service -n Document -o src/Document.Services --rootNamespace WebAppArchitecture

dotnet new entity-ui -n Document -o src/Document.UI --rootNamespace WebAppArchitecture --title "Dokument"

dotnet new entity-resolvers -n Document -o src/Document.Resolvers --rootNamespace WebAppArchitecture --route document
```

## Přidání do solution

```bash
dotnet sln SignFakeArchitecture.sln add src/Document.Models/Document.Models.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.Services/Document.Services.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.UI/Document.UI.csproj
dotnet sln SignFakeArchitecture.sln add src/Document.Resolvers/Document.Resolvers.csproj
```

## Kontrola

```bash
dotnet sln SignFakeArchitecture.sln list
dotnet build SignFakeArchitecture.sln
dotnet run --project src/Apps/Host/Host.csproj
```

Swagger:

```text
http://localhost:8080/swagger
```

## Architecture update 10

See `ARCHITECTURE_UPDATE_10.md` for the centralized DBLayer, secured IQueryable pipeline, DTO projections and EffectiveAccess materialized security projection.


## Layered data access

The current dependency direction is intentionally strict:

`Resolvers / UI / OData -> Domain Services -> AuthorizationLayer -> DBLayer -> ApplicationDbContext -> SQLite`

Domain Services do not inject stores, DbContext, or DBLayer. They receive `IAuthorizationLayer`. DBLayer is the raw persistence library and AuthorizationLayer is the authorization boundary that exposes secured `IQueryable<TEntity>` plus secured mutations. Services project DB entities to DTOs before returning data.

## Update 12: normalized RBAC operations

RBAC operations are normalized through `operations` and `role_type_operations`. Roles and `effective_access` reference RoleTypes by `role_type_id`; runtime authorization no longer parses `operations_csv`. See `ARCHITECTURE_UPDATE_12.md`.


## Update 13 – persistence ownership and operations enrichment

Persistence DB models now live in `src/DBLayer`. Domain `*.Models` projects contain DTO/request contracts only. `AuthorizationLayer` includes bulk operations lookup and post-materialization enrichment extensions; see `ARCHITECTURE_UPDATE_13.md`.

## HK2026 system data seed

On a new database, the host initializes organization/security data from
`_uois/systemdata.hk2026.json`. The default source is GitHub and can be changed
with `SYSTEMDATA_SOURCE`; a local compact seed can be produced with
`tools/extract_systemdata.py`. See `ARCHITECTURE_UPDATE_14.md`.


## Spouštění Hostu a vývojového JsonataPlayground (77.14)

`JsonataPlayground` je knihovna připojovaná do `Host` pouze volitelně. **Nespouštějte** `JsonataPlayground.csproj` pomocí `dotnet run` – nemá vlastní `Main`.

### Lokální vývoj – Debug

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```

Výchozí Debug build připojí `JsonataPlayground`; při `Development` prostředí je UI na `http://localhost:8080/jsonata` a testovací API na `POST /jsonata/execute`. Port se řídí konfigurací Hostu. `ForwardHeaders` a GraphQL `Endpoint` nastavujte výhradně v konfiguraci Hostu (`DigitalProcesses:DataBackend`), ne ve formuláři.

### Vývoj bez playgroundu

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug -p:EnableJsonataPlayground=false
```

### Produkční build a deployment – Release

```bash
dotnet publish src/Apps/Host/Host.csproj -c Release -p:EnableJsonataPlayground=false
```

Release bez explicitního přepnutí playground **nepřipojuje, nekompiluje ani nepublikuje** jako závislost Hostu. Endpointy `/jsonata` se navíc registrují jen když prostředí běží jako `Development`. Při nasazování nepovolujte volbu `EnableJsonataPlayground=true`.

### Build celé solution

```bash
dotnet build SignFakeArchitecture.sln -c Debug
```

`JsonataPlayground.csproj` je nyní knihovna (`OutputType=Library`), takže jej lze sestavovat samostatně i v solution bez chyby CS5001. Stránka je určena pouze do důvěryhodného vývojového prostředí: zobrazované HTTP hlavičky mohou obsahovat cookies a přístupové tokeny.

### Oprava 77.14

Host/Program.cs odstranuje chybne `using WebAppArchitecture.Host;`; `ArchitectureHost` patri do `WebAppArchitecture.Hosting`.


### JSONata import: hlavička Accept (77.15)

Při každém serverovém volání z DigitalProcesses do nakonfigurovaného GraphQL/JSONata
endpointu se nastavuje `Accept: application/json`, i pokud příchozí požadavek
z prohlížeče obsahuje jiný `Accept` nebo žádný. Toto pravidlo sdílí
import formuláře i diagnostická aplikace `/jsonata`; nemění se konfigurace
jednotlivých formulářů ani ostatní přeposílané hlavičky.

Pro kontrolu spusť v režimu Development `/jsonata`, proveď dotaz a v části
„Odchozí HTTP požadavek“ ověř `headers.Accept: ["application/json"]`.

## Import JSON polí a Export dat (77.16)

Pro import kolekce například `query { groups: groupTypeList { id name } }` vytvořte ve **stejné rodičovské sekci jako tlačítko Import dat** sekci `key=groups` a pod ní pole `key=id` a `key=name`. U sekce `groups` nastavte `Max occurrences` na očekávaný maximální počet položek (např. `20`). Definice sekce a jejích polí je jediná; preview si vytváří instance s indexy `0..N-1` a pro každou instanci odděleně ukládá hodnoty dle `FormNode.Id` a cesty instance. JSON odpověď `{ "data": { "groups": [{"id":"1","name":"A"},{"id":"2","name":"B"}] } }` naplní dvě instance.

Nový typ uzlu **Export dat (GraphQL)** (`action.export`) používá stejnou konfiguraci `{ "label", "query", "variables" }` i stejné serverové GraphQL spojení/hlavičky jako import. Před odesláním sesbírá hodnoty celé preview a rekonstruuje objekt `$form`, včetně opakovaných sekcí. Výchozí proměnná `form` se doplní automaticky, pokud ji konfigurace nedefinuje. Například:

```json
{
  "label": "Export dat",
  "query": "mutation Save($input: JSON!) { saveData(input: $input) }",
  "variables": { "input": "$form" }
}
```

Příklad je ilustrativní: názvy GraphQL mutace a skaláru (`JSON`) musí odpovídat schématu JSONata endpointu. Odpověď exportu se **nezapisuje** do polí. Pokud server nevrátí GraphQL `errors` a HTTP transport uspěje, zobrazí se zavíratelné zelené potvrzení; v opačném případě zavíratelné žluté upozornění. Detailní odpověď je v testovacím režimu nadále k dispozici.

Podpora opakovaných instancí a HTTP akcí v této verzi se týká **preview/Test režimu**. Převod na trvale ukládané produkční `Submission` instance, stejně jako pravidla správy změn v čase, je oddělená část runtime implementace a tato verze ji nepřepisuje. Import pracuje s položkami objektových polí, jejichž cesty odpovídají existujícím definicím (např. `groups[0].id`).

Spouštění se nemění:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```

Při nasazování Release je JsonataPlayground nadále vypnut:

```bash
dotnet publish src/Apps/Host/Host.csproj -c Release -p:EnableJsonataPlayground=false
```

## JSON schema a data formulářů (77.17)

Všechny následující endpointy vracejí `application/json` bez ohledu na hlavičku `Accept`:

```http
GET /digital-processes/FormVersionResponse/schema/{formVersionId}
GET /digital-processes/form-preview/data/{formVersionId}/{previewInstanceId}
GET /digital-processes/SubmissionResponse/data/{submissionId}
GET /digital-processes/FormResponse/data/{formDefinitionId}
```

`schema` sestavuje JSON Schema draft 2020-12 z databázových definic FormNode (lokální `key`, rodičovské vazby, `min_occurs`, `max_occurs`, typ uzlu). Příslušné opakované sekce se mapují na `array` s `items`. Návratová data submission používají konkrétní uložené instance SubmissionNode; jejich `parent_id` a `occurrence` určují hierarchii a pořadí, `form_node_id` určuje klíč z definice. Pro opakované struktury vznikají JSON pole.

Testovací endpoint čte dočasný stav preview a rekonstruuje jej stejnou funkcí jako export (`$form`); stav preview není Submission a po expiraci zanikne. Endpoint kolekce `/digital-processes/FormResponse/data/{formDefinitionId}` vyžaduje ID definice formuláře (`FormDefinition.id`), najde všechny její čitelné verze (`FormVersion.form_definition_id`) a vrátí pouze čitelná Submission těchto verzí. Bez ID formuláře není kolekční endpoint dostupný. Jednotlivé záznamy pole jsou rekonstruované JSON dokumenty bez metadat Submission. Vždy je třeba vhodně omezit přístup k osobním údajům. Poznámka: podrobnější JSON Schema validace specifická pro vlastní widgety nemusí být z pouhé definice uzlu odvoditelná.

Vývojové spuštění:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```


## Rekonstruovaná data podle formuláře (77.18)

```http
GET /digital-processes/FormResponse/data/{formDefinitionId}
Accept: application/json
```

`formDefinitionId` je UUID `FormDefinition`, nikoli UUID `FormVersion` nebo `Submission`.
Endpoint vrací pole rekonstruovaných JSON dokumentů ze všech čitelných uložených
Submission vztahujících se k libovolné čitelné verzi této definice formuláře.
Pro neexistující nebo nepřístupnou definici vrací HTTP 404; pro definici bez
čitelných Submission vrací `[]`. Původní nefiltrované
`/digital-processes/SubmissionResponse/data` bylo odstraněno.
Samostatný endpoint `/digital-processes/SubmissionResponse/data/{submissionId}`
zůstává zachován.


## Oprava sestavení 77.19

- `FormDocumentEndpoints.cs`: import `Microsoft.AspNetCore.Builder` pro Minimal API `MapGet`, oprava CS1929.
- `PreviewFormEndpoints.cs`: odstraněna nepoužívaná proměnná `ti` (CS0219).
- Pět `*.Resolvers.csproj` (Role, RoleType, Membership, Group, User): cílené potlačení AD0001 způsobeného pádem `Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer`. Stejný postup jako v DigitalProcesses.Resolvers. Nejde o opravu příčiny v Microsoft analyzátoru a ostatní analyzátory zůstávají aktivní.

Ověření v devcontaineru:

```bash
dotnet clean src/Apps/Host/Host.csproj -c Debug
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apps/Host/Host.csproj -c Debug
```

### RTF templates and submission documents (77.20)

RTF template tags supported by the server-side renderer:

- `[[W:key]] ... [[E]]` selects a nested JSON object.
- `[[F:key]] ... [[E]]` repeats content for an array of JSON objects (nested repeats are supported).
- `[[I:key]]` inserts a scalar from the current JSON object, escaped as RTF.
- `[[CLASSNAME]]` and `[[FILLEDID]]` insert a document label and submission UUID.

Routes (all start with `/digital-processes`):

| Method | Route | Description |
|---|---|---|
| GET | `/FormVersionResponse/rtf-templates/{id}` | List templates for **FormVersion.id** |
| POST | `/FormVersionResponse/rtf-templates/{id}?name=MyTemplate` | Upload raw `application/rtf` (max 5 MB) |
| POST | `/FormVersionResponse/rtf-template/generate/{id}` | Generate and persist an editable starter template from FormNodes |
| GET | `/RtfTemplate/{id}` | Download original template |
| DELETE | `/RtfTemplate/{id}` | Delete template |
| GET | `/RtfTemplate/{id}/submission/{submissionId}` | Render RTF using persisted Submission data |

Example:

```bash
curl -X POST -H 'Content-Type: application/rtf' \
  --data-binary '@template.rtf' \
  'http://localhost:8080/digital-processes/FormVersionResponse/rtf-templates/FORM_VERSION_UUID?name=Custom'
curl -o result.rtf \
  'http://localhost:8080/digital-processes/RtfTemplate/TEMPLATE_UUID/submission/SUBMISSION_UUID'
```

**Persistence:** `DigitalProcesses:RtfTemplates:Directory` specifies a writable *persistent volume* for stored RTFs and their JSON metadata. Default: `app_data/rtf-templates` under the Host content root. This storage is deliberately separate from the EF database; its backup and container volume mount must be configured for production. Each operation checks `FormVersion` Read/Update rights through `IAuthorizationLayer`. Rendering additionally checks the Submission Read permission (`rbacobject_id`) and requires matching form-version ID.

RTF templates in older formats that use other marker conventions, split placeholders across RTF runs, or embed non-text objects may require further compatibility work. Rendering uses the saved SubmissionNode JSON reconstruction and does not call an external service.

## ZArchitecture 77.21 – Applicant visibility, Deny and state transition archive

New policies are authored **in the context of a draft ProcessDefinitionVersion**
(its workflow state and form node), never on the reusable FormNode independently.
The policy is a positive `can_read` flag: an explicitly allowed root section
allows its descendants unless a child explicitly says `false`.
If a state has at least one policy row, root sections without `can_read=true`
are hidden (fail-closed). If it has no rows, legacy visibility is retained.

Endpoints (JSON, normal authentication/RBAC required):

- `GET /digital-processes/ProcessDefinitionVersionResponse/applicant-access/{processVersionId}?stateId={stateId}` – current explicit rules.
- `PUT /digital-processes/ProcessDefinitionVersionResponse/applicant-access/{processVersionId}` with `{"stateId":"<UUID>","formNodeId":"<UUID>","canRead":true}` (or `false`) – update rules for a **draft** process version. Note that the property name in JSON is `canRead` (C# request record).

**Configuration example**: for the `Podano` state, set the root form section
`canRead=true`, then set its `InterniPosouzeni` child `canRead=false`.
Do this before publishing. A new Request / state transition creates a new
Submission RBAC subtree. The Applicant receives explicit bindings to visible
scopes; denied branches get a persistent `RbacDenyRule` on their own Group,
which propagates to all nested Groups. `EffectiveAccess` is reconciled in the
same transactional workflow; normal `AuthorizationLayer.Query(Read)` remains an
indexed SQL filter with no per-node DB round-trips. Existing archived
Submissions retain their original nodes, Groups and Deny records.

The new `RbacDenyRule` also filters derived access obtained through functional
`RbacAccessBinding` (source role Applicant mapped to another operational role).
It does not suppress independent access derived from an unrelated role of the
same user. Other process participants continue to require their appropriate
workflow access rules or inherited scope permissions.

On transition, a privileged, **internal-only** snapshot reader clones the full
submission data even if some nodes are not readable to the acting user. This
reader is not mapped to HTTP; user-facing HTML, JSON and RTF reads must continue
using RBAC-filtered queries. Historical submissions are immutable.

**SQLite upgrade**: startup creates the two new tables on existing SQLite
files using idempotent `CREATE TABLE/INDEX IF NOT EXISTS`; other database
providers require a corresponding deployment migration. Always back up
`data/dump.json` and the database before updating. Running `EnsureCreated`
alone does *not* migrate tables in existing EF Core databases.

**Validation**: Build with `dotnet build src/Apps/Host/Host.csproj -c Debug` and test
(1) Applicant visible section; (2) Applicant hidden section and all descendants
on `/SubmissionResponse/data/{id}`; (3) Dean/referent access as configured;
(4) transition creates a fully populated next Submission but preserves archived
RBAC restrictions; (5) repeated child sections created after Request start.
The ZIP was statically inspected; no .NET SDK is installed in the packaging environment.


## ZArchitecture 78 – sjednocení URI přístupové politiky

- `GET /digital-processes/ProcessDefinitionVersionResponse/applicant-access/{processVersionId}` vrací všechna explicitní pravidla Applicant pro danou verzi procesu. Nepovinný query parametr `stateId` filtruje na konkrétní stav.
- `PUT /digital-processes/ProcessDefinitionVersionResponse/applicant-access/{processVersionId}` nastavuje jedno pravidlo. Tělo JSON: `{"stateId":"<UUID>","formNodeId":"<UUID>","canRead":false}`.
- Oba endpointy odpovídají konvenci `/{app}/{entity}/{operation}/{id}`; předchozí trasy pro `applicant-access` byly odstraněny.
- `RbacDenyRuleModel` je obecný pro jakýkoliv `role_type_id`; Applicant je specializace konfigurace pravidel formuláře, nikoliv omezení RBAC jádra.
- Stávající jiné historické endpointy nejsou touto změnou přejmenovány, aby se nerozbili jejich klienti.


## Core security test suites (verze 78)

Testovací plán, bezpečnostní invarianty, implementované scénáře a backlog jsou v [`tests/README.md`](tests/README.md). První implementovaná sada je v `tests/Core.AuthorizationLayer.Tests` a spouští se `dotnet test tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj -c Debug`.
