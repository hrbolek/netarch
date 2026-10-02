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
│   ├── Base/
│   │   └── Base.csproj
│   │
│   ├── Host/
│   │   ├── Host.csproj
│   │   └── Program.cs
│   │
│   ├── User.Models/
│   ├── User.Services/
│   ├── User.UI/
│   └── User.Resolvers/
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
- `IBaseService<TDBModel,TDTOModel>` a `BaseService`,
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
public interface IUserService : IBaseService<UserModel, UserResponse>
{
}

public sealed class UserService
    : BaseService<UserModel, UserResponse, IUserStore>, IUserService
{
    public UserService(IUserStore store) : base(store)
    {
    }
}
```

Standardní CRUD operace poskytuje `BaseService`. Konkrétní service může kdykoliv přidat další entity-specific business metody.

Create a Update mohou používat různé payload typy. `BaseService` proto obsahuje generické metody ve stylu:

```csharp
CreateAsync<TCreateRequest>(...)
UpdateAsync<TUpdateRequest>(...)
```

Payload implementuje příslušný kontrakt a service jej převede na standardní store model.

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
dotnet run --project src/Host/Host.csproj
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

Do `src/Host/Program.cs` přidejte namespace:

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
dotnet run --project src/Host/Host.csproj
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
    : IBaseService<DocumentModel, DocumentResponse>
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
src/Base/Security/EntraEndpointSecurity.cs
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
dotnet run --project src/Host/Host.csproj
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
dotnet run --project src/Host/Host.csproj
```

Swagger:

```text
http://localhost:8080/swagger
```
