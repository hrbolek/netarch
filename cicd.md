# CI/CD, interní NuGet a nasazení do testovacího prostředí

Tento dokument popisuje doporučený způsob sdílení společných knihoven, řízení verzí, CI/CD a automatického nasazování aplikací postavených nad architekturou `WebAppArchitecture`.

Cílem je oddělit:

- vývoj společných architektonických knihoven,
- vývoj konkrétních doménových entit a aplikací,
- distribuci stabilních knihoven přes interní NuGet feed,
- sestavení a testování aplikací,
- automatické nasazení do testovacího prostředí.

---

# 1. Základní princip

V rámci jedné solution je během vývoje přirozené používat:

```xml
<ProjectReference Include="../Base/Base.csproj" />
```

Jakmile se však některá část architektury stane sdílenou komponentou používanou více řešeními nebo více týmy, je vhodné ji publikovat jako interní NuGet balíček.

Doporučený cílový model:

```text
Git repository
    │
    ▼
CI pipeline
    │
    ├── restore
    ├── build
    ├── test
    └── pack
          │
          ▼
Internal NuGet Feed
          │
          ├── WebAppArchitecture.Base
          ├── WebAppArchitecture.Security
          ├── WebAppArchitecture.Data
          └── ...
                    │
                    ▼
           Application repository
                    │
                    ├── restore packages
                    ├── build
                    ├── test
                    ├── container build
                    └── deploy TEST
```

NuGet feed se tím stává verzovanou distribuční vrstvou mezi architekturou a konkrétními aplikacemi.

---

# 2. Co má být NuGet balíček

Ne všechny projekty solution je vhodné publikovat jako samostatný NuGet.

Doporučené rozdělení:

| Projekt / vrstva | Interní NuGet | Poznámka |
|---|---:|---|
| `Base` | ano | hlavní sdílená architektura |
| společné security/RBAC knihovny | ano | pokud jsou oddělené |
| společné filtering / endpoint abstractions | ano nebo součást `Base` | podle velikosti |
| konkrétní `*.Models` | většinou ne | patří konkrétní doméně |
| konkrétní `*.Services` | většinou ne | aplikační business logika |
| konkrétní `*.UI` | většinou ne | UI konkrétní entity/aplikace |
| konkrétní `*.Resolvers` | většinou ne | API konkrétní entity |
| `Host` | ne | spustitelná aplikace |
| templates | samostatný package nebo repo | viz níže |

Pro současný projekt je přirozeným prvním interním balíčkem:

```text
WebAppArchitecture.Base
```

Ten může obsahovat například:

```text
Data
Services
Resolvers
Endpoints
Filtering
Security / Entra ID
Base UI components
```

Doménové projekty pak používají:

```xml
<PackageReference Include="WebAppArchitecture.Base" />
```

místo:

```xml
<ProjectReference Include="../Base/Base.csproj" />
```

---

# 3. Kdy použít ProjectReference a kdy PackageReference

## ProjectReference

Používejte, pokud:

- projekty jsou součástí jedné solution,
- mění se současně,
- potřebujete okamžitě ladit zdrojové kódy napříč projekty,
- jde o jednu aplikaci nebo jeden vývojový celek.

Příklad:

```xml
<ProjectReference Include="../User.Models/User.Models.csproj" />
```

## PackageReference

Používejte, pokud:

- komponentu používá více solution,
- komponenta má vlastní release cycle,
- chcete stabilní verzované API,
- chcete reprodukovatelné buildy,
- nechcete, aby konzument automaticky dostal nejnovější rozpracovaný kód.

Příklad:

```xml
<PackageReference Include="WebAppArchitecture.Base" Version="1.4.0" />
```

Doporučený princip:

```text
uvnitř aplikační domény
    -> ProjectReference

mezi samostatně verzovanými produkty / solution
    -> PackageReference
```

---

# 4. Firemní NuGet feed

Pro institucionální použití je vhodný interní NuGet-compatible package store.

Možnosti:

- Azure Artifacts,
- GitHub Packages,
- GitLab Package Registry,
- Sonatype Nexus Repository,
- JFrog Artifactory,
- vlastní NuGet server.

Z architektonického pohledu není zásadní, který produkt bude použit.

Důležité je, aby podporoval:

```text
NuGet v3 feed
authentication
versioning
package retention
permissions
audit
CI push
developer restore
```

---

# 5. Doporučená struktura package feedů

Ve větší instituci je vhodné rozlišit minimálně dva kanály:

```text
nuget-dev
nuget-release
```

případně:

```text
nuget-dev
nuget-test
nuget-release
```

Příklad:

```text
WebAppArchitecture.Base
    1.4.0-dev.125
    1.4.0-dev.126
    1.4.0-rc.1
    1.4.0
    1.4.1
```

Pro produkční aplikace by se měly používat pouze stabilní verze:

```text
1.4.0
1.4.1
1.5.0
```

Testovací prostředí může používat:

```text
1.5.0-rc.2
```

nebo přesně definovaný development build.

---

# 6. Verzování balíčků

Doporučené je Semantic Versioning:

```text
MAJOR.MINOR.PATCH
```

Například:

```text
1.4.2
```

Význam:

```text
MAJOR
breaking change

MINOR
nová zpětně kompatibilní funkcionalita

PATCH
oprava chyby
```

Pre-release verze:

```text
1.5.0-alpha.1
1.5.0-beta.3
1.5.0-rc.1
```

Pro CI buildy lze používat například:

```text
1.5.0-dev.245
```

kde `245` může být číslo pipeline nebo GitHub Actions run number.

---

# 7. Package metadata v projektu

Například `Base.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <PackageId>WebAppArchitecture.Base</PackageId>
    <Authors>Organization</Authors>
    <Description>
      Shared architecture components for WebAppArchitecture.
    </Description>

    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

</Project>
```

Verzi je lepší řídit z CI než ji ručně přepisovat v každém `.csproj`.

Například:

```bash
dotnet pack src/Base/Base.csproj \
    -c Release \
    -p:PackageVersion=1.4.0
```

---

# 8. Central Package Management

Pro větší solution je vhodné používat:

```text
Directory.Packages.props
```

Například:

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>

    <PackageVersion
        Include="WebAppArchitecture.Base"
        Version="1.4.0" />

    <PackageVersion
        Include="Microsoft.Identity.Web"
        Version="3.14.1" />

    <PackageVersion
        Include="Swashbuckle.AspNetCore"
        Version="6.6.2" />

  </ItemGroup>

</Project>
```

V konkrétním `.csproj` pak:

```xml
<PackageReference Include="WebAppArchitecture.Base" />
```

bez verze.

Výhoda:

```text
jedna solution
    ->
jedno místo řízení verzí
```

---

# 9. NuGet.config

Doporučené je mít v repository explicitní:

```text
NuGet.config
```

Příklad:

```xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>

  <packageSources>

    <clear />

    <add
      key="company"
      value="https://nuget.example.cz/v3/index.json" />

    <add
      key="nuget.org"
      value="https://api.nuget.org/v3/index.json" />

  </packageSources>

</configuration>
```

Credentialy nikdy neukládejte přímo do repository.

Autentizace se má řešit pomocí:

- CI secrets,
- environment variables,
- credential provideru,
- developer loginu,
- workload identity / service principalu.

---

# 10. Vývoj v Dev Containeru

Dev Container by měl mít přístup ke stejnému package feedu jako CI.

Standardní workflow:

```bash
dotnet restore
dotnet build
dotnet test
```

Pokud je interní feed autentizovaný, credentialy je vhodné přenést do containeru bezpečným způsobem.

Není vhodné zapisovat token například do:

```text
.devcontainer/devcontainer.json
```

pokud je tento soubor verzován.

Preferujte:

```text
host environment
CI secret
credential helper
developer secret store
```

---

# 11. Templates jako sdílený artefakt

Templates mají jiný charakter než běžná runtime knihovna.

Aktuálně mohou být instalovány lokálně:

```bash
dotnet new install ./templates
```

Pro institucionální distribuci existují dva vhodné modely.

## Varianta A – templates ve společném Git repository

Vývojář:

```bash
git clone ...
dotnet new install ./templates
```

Jednoduché pro počáteční fázi.

## Varianta B – template NuGet package

.NET templates lze distribuovat také jako NuGet package.

Pak může být interně publikován například:

```text
WebAppArchitecture.Templates.1.4.0.nupkg
```

a instalace:

```bash
dotnet new install WebAppArchitecture.Templates
```

To je vhodnější pro stabilní institucionální distribuci.

Doporučené verzování:

```text
WebAppArchitecture.Base       1.4.0
WebAppArchitecture.Templates  1.4.0
```

pokud daná verze templates očekává konkrétní kontrakty `Base`.

Tím se snižuje riziko, že vývojář použije novou template proti starému `Base`.

---

# 12. Doporučené oddělení repository

S růstem řešení lze přejít na strukturu:

```text
webapp-architecture
    |
    +-- Base
    +-- Templates
    +-- tests
    +-- CI

application-signfake
    |
    +-- User.*
    +-- Document.*
    +-- Host
    +-- application tests
    +-- deployment
```

`webapp-architecture` publikuje:

```text
WebAppArchitecture.Base
WebAppArchitecture.Templates
```

a konkrétní aplikace je pouze konzumuje.

Výhoda:

```text
architektura má vlastní životní cyklus
aplikace má vlastní životní cyklus
```

---

# 13. CI pipeline pro architektonický balíček

Pipeline pro `WebAppArchitecture.Base` by měla provádět:

```text
checkout
    ↓
restore
    ↓
build
    ↓
unit tests
    ↓
pack
    ↓
push NuGet package
```

Publikace balíčku by měla nastat pouze při splnění definovaných podmínek.

Například:

```text
push do feature branch
    -> build + test

push do main
    -> build + test + pre-release package

Git tag v1.4.0
    -> build + test + release package
```

---

# 14. Příklad GitHub Actions – build a test

Soubor:

```text
.github/workflows/build.yml
```

Příklad:

```yaml
name: Build

on:
  push:
    branches:
      - main

  pull_request:

jobs:

  build:

    runs-on: ubuntu-latest

    steps:

      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: >
          dotnet build
          --configuration Release
          --no-restore

      - name: Test
        run: >
          dotnet test
          --configuration Release
          --no-build
```

V reálné instituci je vhodné připnout používané GitHub Actions na schválené verze nebo commit SHA podle interních bezpečnostních pravidel.

---

# 15. CI pipeline pro NuGet package

Příklad release workflow:

```text
.github/workflows/package.yml
```

```yaml
name: Package

on:
  push:
    tags:
      - 'v*'

jobs:

  package:

    runs-on: ubuntu-latest

    permissions:
      contents: read
      packages: write

    steps:

      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: >
          dotnet build
          --configuration Release
          --no-restore

      - name: Test
        run: >
          dotnet test
          --configuration Release
          --no-build

      - name: Resolve version
        shell: bash
        run: |
          VERSION="${GITHUB_REF_NAME#v}"
          echo "PACKAGE_VERSION=$VERSION" >> "$GITHUB_ENV"

      - name: Pack
        run: >
          dotnet pack
          src/Base/Base.csproj
          --configuration Release
          --no-build
          -p:PackageVersion=${{ env.PACKAGE_VERSION }}
          --output ./artifacts/packages

      - name: Push package
        run: >
          dotnet nuget push
          "./artifacts/packages/*.nupkg"
          --source "${{ secrets.NUGET_SOURCE }}"
          --api-key "${{ secrets.NUGET_API_KEY }}"
          --skip-duplicate
```

Konkrétní autentizační mechanismus závisí na použitém package store.

---

# 16. Release přes Git tag

Doporučený release postup:

```bash
git checkout main
git pull

git tag v1.4.0
git push origin v1.4.0
```

Tag:

```text
v1.4.0
```

spustí pipeline, která vytvoří:

```text
WebAppArchitecture.Base.1.4.0.nupkg
```

a publikuje jej do interního feedu.

Git tag tak přesně určuje:

```text
zdrojový commit
    =
verze package
```

---

# 17. GitHub Release

GitHub Release může být vytvořen nad stejným tagem:

```text
v1.4.0
```

Doporučený význam:

```text
Git tag
    -> technická identita verze

GitHub Release
    -> changelog + informace pro lidi

NuGet package
    -> distribuční artefakt pro aplikace
```

Není vhodné používat GitHub Release attachment jako náhradu NuGet feedu.

---

# 18. CI pipeline konkrétní aplikace

Aplikační repository používající interní package má pipeline:

```text
checkout
    ↓
authenticate to package feed
    ↓
restore
    ↓
build
    ↓
test
    ↓
container build
    ↓
push container image
    ↓
deploy TEST
```

---

# 19. Container image

`Host` je vhodné distribuovat jako Docker/OCI image.

Například:

```text
registry.example.cz/
    webapparchitecture/signfake:1.8.3
```

nebo pro development:

```text
registry.example.cz/
    webapparchitecture/signfake:main-431
```

CI:

```bash
docker build \
    -t registry.example.cz/webapparchitecture/signfake:${VERSION} \
    .

docker push \
    registry.example.cz/webapparchitecture/signfake:${VERSION}
```

Vhodnější je používat build metadata odvozená z:

```text
Git commit SHA
Git tag
CI run number
```

---

# 20. Doporučené image tagy

Nedoporučuje se spoléhat pouze na:

```text
latest
```

Lepší:

```text
1.8.3
1.8
main-431
sha-a3fc129
```

Jedna image může mít více tagů:

```text
signfake:1.8.3
signfake:1.8
signfake:sha-a3fc129
```

Pro testovací prostředí je vhodné ukládat **konkrétní immutable tag** nebo digest.

---

# 21. Automatické nasazení do TEST

Po úspěšném:

```text
build
test
container push
```

může pipeline automaticky nasadit image do testovacího prostředí.

Například:

```text
main
    ↓
CI
    ↓
image main-431
    ↓
TEST environment
```

Produkční prostředí by typicky nemělo být automaticky aktualizováno každým commitem do `main`.

---

# 22. Doporučené prostředí

Minimálně:

```text
DEV
TEST
PROD
```

Význam:

## DEV

```text
lokální vývoj
Dev Container
InMemory Store
development Entra configuration
```

## TEST

```text
automaticky nasazovaná aplikace
reálný container runtime
testovací Entra registrace
testovací DB
integrační testy
```

## PROD

```text
řízený release
schválený package
schválená container image
produkční identity
produkční secrets
```

---

# 23. Deployment konfigurace

Aplikační image by měla být stejná pro TEST i PROD.

Měnit se má konfigurace:

```text
container image
    stejná

configuration
    jiná
```

Například:

```text
TEST
AzureAd__TenantId=...
ConnectionStrings__Default=...

PROD
AzureAd__TenantId=...
ConnectionStrings__Default=...
```

Image proto nemá obsahovat environment-specific secrets.

---

# 24. Secrets

Do Git repository nepatří:

```text
NuGet token
registry password
database password
Entra client secret
deployment credentials
```

Používejte:

```text
GitHub Actions Secrets
Azure DevOps Variable Groups
Vault
Kubernetes Secrets
cloud secret store
```

Ideální je používat workload identity / federovanou identitu a dlouhodobé statické tokeny minimalizovat.

---

# 25. CI/CD a Entra ID

Testovací prostředí by mělo používat samostatnou Entra aplikaci / konfiguraci od produkce.

Například:

```text
WebAppArchitecture-TEST
WebAppArchitecture-PROD
```

TEST konfigurace:

```text
AzureAd__TenantId
AzureAd__ClientId
```

se nastavuje deploymentem, nikoli buildem.

Pipeline tedy nevytváří jiný binární artefakt pro Entra TEST a PROD.

---

# 26. Database migrations

Pokud se později přejde z InMemory Store na databázovou implementaci, CI/CD musí řešit také migrace.

Doporučený tok:

```text
deploy infrastructure dependency
    ↓
run DB migration
    ↓
deploy application
    ↓
health check
```

Migrace mají být:

- verzované,
- součástí repository,
- testované,
- pokud možno zpětně kompatibilní během rolling deploymentu.

---

# 27. Health check

Testovací prostředí by mělo mít health endpoint, například:

```text
/health
```

Pipeline po deploymentu ověří:

```bash
curl --fail https://test.example.cz/health
```

Nasazení není úspěšné pouze proto, že container nastartoval.

Úspěšný deployment znamená:

```text
container běží
+
health endpoint odpovídá
+
základní smoke test projde
```

---

# 28. Smoke test po deploymentu

Po nasazení do TEST je vhodné spustit například:

```text
GET /health
GET /architecture/User/list
POST /architecture/User/vector
```

podle toho, které endpointy jsou v daném prostředí veřejné nebo dostupné testovací identitě.

Smoke test má ověřit minimálně:

```text
routing
DI
service
store
serialization
authentication
```

---

# 29. Doporučená pipeline TEST prostředí

Kompletní tok:

```text
Developer
    │
    ▼
Pull Request
    │
    ├── restore
    ├── build
    └── tests
    │
    ▼
Merge to main
    │
    ├── restore
    ├── build
    ├── tests
    ├── container build
    ├── container push
    ├── deploy TEST
    ├── health check
    └── smoke tests
```

Pokud je změněn také architektonický balíček:

```text
Architecture repository
    │
    ▼
new NuGet version
    │
    ▼
Application updates PackageReference
    │
    ▼
application CI/CD
```

---

# 30. Produkční release

Doporučený postup:

```text
TEST successfully validated
    ↓
release tag
    ↓
approval
    ↓
promote same container image
    ↓
PROD
```

Důležité:

> Do produkce se má nasadit stejný image, který byl otestován v TEST prostředí.

Ne:

```text
TEST -> build image A
PROD -> rebuild image B
```

ale:

```text
build image A
    ├── TEST
    └── PROD
```

---

# 31. Package promotion

Stejný princip lze použít i pro NuGet.

Například:

```text
dev feed
    ↓
validation
    ↓
release feed
```

Není nutné package znovu buildovat.

Preferovaný princip:

```text
build once
promote many
```

---

# 32. Závislost aplikace na Base

Každá aplikační verze by měla mít jednoznačně zjistitelnou verzi:

```text
WebAppArchitecture.Base
```

Například:

```xml
<PackageReference
    Include="WebAppArchitecture.Base"
    Version="1.4.0" />
```

Díky tomu je možné zpětně zjistit:

```text
SignFake 1.8.3
    používá
WebAppArchitecture.Base 1.4.0
```

To výrazně zlepšuje diagnostiku a auditovatelnost.

---

# 33. Automatické update dependency

Později lze použít nástroje typu Dependabot nebo Renovate.

Ty mohou automaticky vytvořit Pull Request:

```text
WebAppArchitecture.Base
1.4.0 -> 1.5.0
```

CI na tomto PR provede:

```text
restore
build
tests
```

a vývojář pouze rozhodne, zda novou verzi přijme.

Nedoporučuje se používat nekontrolované wildcard verze typu:

```xml
Version="1.*"
```

pro klíčovou interní architekturu.

---

# 34. Templates a Base kompatibilita

Protože templates generují kód závislý na kontraktech `Base`, je vhodné jejich kompatibilitu explicitně řídit.

Například:

```text
WebAppArchitecture.Base       1.4.x
WebAppArchitecture.Templates  1.4.x
```

Template release notes mohou uvádět:

```text
Requires WebAppArchitecture.Base >= 1.4.0 < 2.0.0
```

Při breaking change v `Base`:

```text
Base 2.0
```

má vzniknout také odpovídající nová verze templates.

---

# 35. Testy templates

CI projektu architektury by měla testovat nejen `Base`, ale také templates.

Doporučený test:

```text
install local template
    ↓
generate Demo entity
    ↓
add generated projects to temporary solution
    ↓
dotnet restore
    ↓
dotnet build
```

Tím lze automaticky zachytit přesně ty chyby, které při vývoji vznikají při nesouladu:

```text
Base
vs
template
```

Pseudopipeline:

```bash
dotnet new install ./templates

dotnet new entity-model \
    -n Demo \
    -o ./tmp/Demo.Models \
    --rootNamespace WebAppArchitecture

dotnet new entity-service \
    ...

dotnet new entity-ui \
    ...

dotnet new entity-resolver \
    ...

dotnet build ./tmp/Demo.sln
```

Tento test má pro architektonický framework velmi vysokou hodnotu.

---

# 36. Doporučené CI kontroly

Pull Request do architektonického repository by měl projít:

```text
dotnet restore
dotnet build
dotnet test
template generation test
generated entity build
format / analyzers
package build test
```

Až potom merge.

---

# 37. Branch strategie

Není nutné zavádět komplikovaný Git Flow.

Pro většinu týmů postačí:

```text
feature/*
    ↓
Pull Request
    ↓
main
```

`main` má být vždy buildovatelný.

Release je označen tagem:

```text
v1.4.0
```

---

# 38. Doporučené status checks

Pro merge do `main` vyžadovat například:

```text
Build
Unit tests
Template tests
Security scan
Code review
```

Podle kritičnosti projektu lze přidat:

```text
dependency scan
container scan
SAST
license scan
```

---

# 39. Artefakty CI

Pipeline může archivovat:

```text
NuGet packages
test results
coverage reports
SBOM
container metadata
deployment manifest
```

Každý artefakt by měl být spojen s:

```text
Git commit SHA
CI run ID
version
```

---

# 40. Software Bill of Materials

Pro institucionální vývoj je vhodné do budoucna generovat SBOM.

Umožňuje evidovat:

```text
aplikace
    ↓
interní packages
    ↓
externí NuGet packages
    ↓
konkrétní verze
```

To je důležité pro:

```text
security incident response
vulnerability management
audit
supply-chain security
```

---

# 41. Doporučená cílová architektura CI/CD

```text
                 ┌─────────────────────────┐
                 │ Architecture repository │
                 │                         │
                 │ Base                    │
                 │ Templates               │
                 └────────────┬────────────┘
                              │
                              │ CI
                              ▼
                 ┌─────────────────────────┐
                 │ Internal NuGet          │
                 │                         │
                 │ Base 1.4.0              │
                 │ Templates 1.4.0         │
                 └────────────┬────────────┘
                              │
                              │ PackageReference
                              ▼
                 ┌─────────────────────────┐
                 │ Application repository  │
                 │                         │
                 │ Models                  │
                 │ Services                │
                 │ UI                      │
                 │ Resolvers               │
                 │ Host                    │
                 └────────────┬────────────┘
                              │
                              │ CI
                              ▼
                 ┌─────────────────────────┐
                 │ Container Registry      │
                 │                         │
                 │ app:sha-a3fc129         │
                 └────────────┬────────────┘
                              │
                   ┌──────────┴──────────┐
                   │                     │
                   ▼                     ▼
            ┌─────────────┐       ┌─────────────┐
            │ TEST        │       │ PROD        │
            │ automatic   │       │ controlled  │
            └─────────────┘       └─────────────┘
```

---

# 42. Doporučený první krok pro tento projekt

Není nutné okamžitě rozdělit celé repository.

První praktická iterace může být:

1. ponechat současnou solution,
2. označit `Base.csproj` jako packable,
3. vytvořit interní NuGet feed,
4. připravit CI pro `dotnet build`, `dotnet test`, `dotnet pack`,
5. publikovat `WebAppArchitecture.Base`,
6. přidat automatický test templates,
7. vytvořit container image `Host`,
8. pushnout image do interní registry,
9. automaticky nasadit image do TEST,
10. po stabilizaci oddělit architektonický framework do samostatného repository.

Tento postup umožňuje evoluci bez nutnosti okamžité reorganizace všech projektů.

---

# 43. Doporučené fáze zavedení

## Fáze 1 – CI

```text
build
tests
template build test
```

## Fáze 2 – package management

```text
internal NuGet
Base package
Templates package
semantic versioning
```

## Fáze 3 – container registry

```text
Host Docker image
immutable tags
```

## Fáze 4 – TEST deployment

```text
automatic deployment
health checks
smoke tests
```

## Fáze 5 – controlled PROD

```text
approval
image promotion
audit
rollback
```

---

# 44. Rollback

Rollback má být jednoduchý výběr předchozího image:

```text
signfake:1.8.3
    ↓
problém

rollback
    ↓
signfake:1.8.2
```

Stejně u interní knihovny aplikace používá konkrétní verzi:

```text
Base 1.4.0
```

a upgrade na:

```text
Base 1.5.0
```

je explicitní změna v repository.

---

# 45. Závěr

Pro tuto architekturu je vhodné chápat interní NuGet jako **hranici mezi frameworkem a aplikací**.

```text
WebAppArchitecture.Base
    =
verzovaný interní framework

Templates
    =
generátor kódu kompatibilní s frameworkem

Entity projects
    =
konkrétní doména

Host
    =
deployovatelná aplikace
```

Doporučený institucionální řetězec:

```text
Git
    ↓
CI
    ↓
Internal NuGet
    ↓
Application Build
    ↓
Tests
    ↓
Container Registry
    ↓
Automatic TEST Deployment
    ↓
Validation
    ↓
Controlled PROD Deployment
```

Klíčové principy:

- sdílené knihovny distribuovat jako verzované NuGet packages,
- uvnitř jedné domény používat `ProjectReference`,
- mezi samostatně verzovanými řešeními používat `PackageReference`,
- templates verzovat společně s architektonickým kontraktem,
- testovat automaticky, že template vygeneruje buildovatelnou entitu,
- buildovat artefakt pouze jednou,
- stejný container image propagovat přes TEST až do PROD,
- secrets držet mimo repository,
- každou produkční verzi spojit s konkrétním Git commitem, package verzí a container imagem.
