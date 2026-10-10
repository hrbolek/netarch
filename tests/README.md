# ZArchitecture – testovací strategie Core (v78)

## Cíl a přejímací kritéria

Všechna testovací řešení žijí výhradně v `tests/`. Cílem je zabránit regresím v `SecurityLayer`, materializaci `EffectiveAccess`, řízení rolí, hierarchickém RBAC, řízení zákazů a databázově zabezpečeném čtení. **P0 testy musejí projít všechny před přijetím změny autorizačního jádra.** Očekává se nula neoprávněných zpřístupnění a ekvivalence úplného a inkrementálního přepočtu.

## Původní plán – 15 sad, 194 scénářů

| Sada | Téma | Plánovaný počet | Priorita | Stav |
|---|---|---:|---|---|
| T01 | Databázové modely a integrita | 12 | P1 | Etapa 3 |
| T02 | Hierarchie skupin, materializované cesty | 14 | P0 | Částečně implementováno |
| T03 | RoleType, operace a oddělení CRUD | 12 | P0 | Částečně implementováno |
| T04 | Membership, časové intervaly | 12 | P0 | Etapa 2 |
| T05 | Delegování / zastupování | 10 | P0 | Etapa 2 |
| T06 | RbacAccessBinding | 12 | P0 | Částečně implementováno |
| T07 | EffectiveAccess, inkrementální a úplná projekce | 16 | P0 | Částečně implementováno |
| T08 | RbacDenyRule, rekurze a izolace rolí | 20 | P0 | Částečně implementováno |
| T09 | SecurityLayer.Query a SQL autorizace | 14 | P0 | Částečně implementováno |
| T10 | Zabezpečený CRUD | 12 | P0 | Etapa 2 |
| T11 | Správa RBAC Services a Minimal API | 16 | P1 | Etapa 3 |
| T12 | Transakce, souběh a rollback | 12 | P0 | Etapa 2 |
| T13 | Identita a autentizace | 10 | P0 | Etapa 3 |
| T14 | Výkon, škálování, SQL plán | 10 | P1 | Etapa 3 |
| T15 | Provozní integrační scénáře | 14 | P0 | Etapa 3 |

Plánované počty se mohou překrývat u parametrizovaných testů; jako přejímací kritérium se evidují jednotlivé spuštěné případy, ne pouze počet metod `[Fact]`.

## Etapa 1 – bezpečnostní jádro (T02, T03, T06–T09)

Projekt: `tests/Core.SecurityLayer.Tests`. Spuštění: `dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Debug`.

### Povinný backlog do úplného dokončení Etapy 1

- **T02:** vytvoření nové hluboké hierarchie, `Group.path` po přesunu celého podstromu, zákaz cyklu, odstranění používané skupiny, dědění při opakovaném zapnutí `inherit_roles`.
- **T03:** vytvoření role a typu role přes Services, více operací, duplicity, zrušení jednotlivých CRUD povolení, změna role za běhu.
- **T06:** expirace bindingu, změna zdrojového typu, více bindingů na jeden cíl, závod při odebrání vazby, neautorizovaná správa.
- **T07:** úplná kombinatorika přidání/odebrání členství a časové platnosti, souběh a obnovování projekce, duplicitní zdroje, inkrementální projekce v náhodně generovaných stromech.
- **T08:** přesun podstromu se zákazem, změna zakázané role, mazání pravidel s rollbackem, přechod přes hranici `inherit_roles=false`, atomické promítnutí změn.
- **T09:** SQL překládání pro různé entity, `List`, `Create`, `Update`, `Delete`, kombinace stránkování, obohacení o operace, `DbCommandInterceptor` s ověřením jediného SQL dotazu.

## Realistický profil výkonu

Small: 100 uživatelů / 1 000 RBAC objektů / 500 rolí. Medium: 5 000 / 50 000 / 20 000. Large: 20 000 / 200 000 / 100 000. Neslibujeme prahové časy bez měření. Běžný databázově filtrovaný dotaz musí mít počet SQL příkazů nezávislý na počtu záznamů.

## Technologie

.NET 8, xUnit, Microsoft.NET.Test.Sdk, skutečný SQLite EF Core provider. Všechny testy běží izolovaně. Vývojová SQLite in-memory nesmí být nahrazena EF InMemory, protože potřebujeme ověřovat skutečný SQL překlad a relační integritu. Pro concurrency se v další etapě použije souborová SQLite.

## Bezpečnost při změně Request/Submission

Kritická integrační sada T15 v další etapě ověří: student vidí Request, ale nikoli interní sekci a její potomky; děkan ji vidí na základě vlastní role; při změně stavu Request vznikne nové Submission s novým RBAC podstromem a Deny; staré Submission zůstává s původními pravidly a úplnými daty. Úplná kopie dat nesmí vznikat z uživatelsky filtrovaného JSON.

## Stav implementace

Implementované testy a jejich podrobné kontrakty: [Core.SecurityLayer.Tests/README.md](Core.SecurityLayer.Tests/README.md). Testovací plán není totéž co hotové pokrytí. Před mergem je třeba spustit `dotnet test` v devcontaineru a odstranit případné regresní chyby; tato distribuce nebyla sestavena v prostředí s .NET SDK.

## Aktualizace Etapy 1 – doplnění integračních regresních testů

Do `Core.SecurityLayer.Tests/AdditionalAuthorizationTests.cs` přibylo **20 nových testovacích metod**, z nichž parametrizovaný test CRUD matice přidává čtyři samostatné běhové případy; celkem **23 nových případů**. Rozšiřují T02, T03, T06, T07, T08 a T09. Přesný přehled, očekávané výsledky, bezpečnostní invarianty a otevřená témata jsou v [Core.SecurityLayer.Tests/README.md](Core.SecurityLayer.Tests/README.md).

Aktuální stav nadále označujeme **částečným pokrytím Etapy 1** – neprohlašujeme 86 plánovaných scénářů za dokončené. Testy nejsou v této distribuci ověřeny kompilací ani spuštěním; chybí zde .NET SDK.

## Opravy po prvním reálném spuštění testů (fix4)

První běh na .NET 8: 57 testů, 40 úspěšných, 17 neúspěšných.
Příčiny zjištěné z trasování:

- `RbacFixture.Operation` používal náhodné UUID místo stabilního `RbacOperationIds.FromName`; tím se nespojilo mapování operací s dotazy `SecurityLayer.Query`.
- `SecurityProjectionService.RebuildAllAsync` prováděl `ExecuteDelete` mimo EF change tracker a pak vytvářel nové řádky se shodným složeným klíčem. Oprava provádí tracked odstranění, `SaveChanges`, a až následně vložení nové projekce v téže transakci.
- `DBLayer.StageUpdateAsync` připojoval druhou instanci již sledované entity se stejným primárním klíčem; oprava používá `CurrentValues.SetValues` pro již sledovanou instanci.
- Dva testy hierarchie používaly `DbSet.Update(record with {...})` i při existující sledované instanci. Opraveny na změnu sledovaných hodnot.

Předchozí testovací výsledky nelze považovat za potvrzení správnosti autorizační logiky. Po opravě je nutné **znovu spustit celý testovací projekt** a prověřit případná zbývající selhání.

```bash
dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Debug
```


## Oprava fix6 – SQL filtrování T09

Pět posledních testů T09 v předchozím běhu vracelo prázdné množiny,
ačkoli projekce rolí byla funkční. `SecurityLayer.Query<TEntity>` je nově
vyjádřen jako databázový poddotaz `accessibleObjects.Contains(entity.rbacobject_id)`
s explicitním joinem `EffectiveAccessModel` a `RoleTypeOperationModel`.
Nedochází k načtení ID do paměti ani k obcházení RBAC.

Ověřte: `dotnet test tests/Core.SecurityLayer.Tests/Core.SecurityLayer.Tests.csproj -c Debug`.
Změna není v tomto prostředí kompilací ověřena.


## Etapa 2 – T04, T05, T10, T12 (první implementace)

Nový `Core.SecurityLayer.Tests/Stage2LifecycleTests.cs` obsahuje 18 izolovaných relačních testů.

| Sada | Scénáře a invarianty |
|---|---|
| T04 | Nezávislost Membership a Role: vytvoření, budoucí začátek, expirace a odebrání Membership nesmějí měnit Role ani projekci `EffectiveAccess`. Testy porovnávají kompletní sémantický snímek projekce před/po změně členství. |
| T05 | Role označená `is_deputy`; časový rozsah; odebrání zastupující role bez dopadu na původního nositele; `Deny` vůči zastupující roli. |
| T10 | `Read` neopravňuje k `Update`/`Delete`; `Update` smí změnit povolený objekt; cizí RBAC kontext se nesmí aktualizovat. |
| T12 | Rollback role i zákazu při chybě po `SaveChanges`; opakovaná synchronizace bez změny projekce a bez obnovení zakázané role. |

### Důležitá omezení pokrytí

- **T04 – nově stanovený kontrakt:** Membership je zcela nezávislé na Role. Žádná změna Membership nesmí vytvořit, odebrat nebo časově omezit oprávnění odvozená z Role. Produkční `SecurityProjectionService.CalculateRows()` byl opraven: Membership není vstupem výpočtu `EffectiveAccess`. Testy ponechávají úplné porovnání projekce.
- **T05:** `is_deputy` je v současném `SecurityProjectionService` projekčně ekvivalentní běžné roli. Validace, *kdo smí vytvořit zástup*, kontrola `delegated_by_role_id` a zákaz privilegovaného řetězení delegací zatím nejsou zajištěny projekční službou; potřebují explicitní aplikační kontrakt a následné testy.
- **T10:** `SecurityLayer.StageCreateAsync()` nyní pouze deleguje na DBLayer bez ověření operace `Create`. Dokud není jednoznačně stanovena vazba vytvořené entity na nadřazený autorizační kontext, **nelze pokrytí zabezpečeného Create označit za hotové**. `Update` dnes kontroluje cílové `rbacobject_id`, ale zvláštní pravidla pro jeho změnu mohou vyžadovat další guard.
- **T12:** Zde jsou především testy atomicity rollbacku; skutečná paralelní zátěž více SQLite připojení a konflikt souběžných zápisů je navazující úloha.

Průchod Etapou 1 **57/57** oznámil uživatel pro fix6. Nové testy Etapy 2 v tomto artefaktu dosud nejsou zkompilovány ani spuštěny.


### Oprava kontraktu T04 – nezávislost Membership/Role (10. 10. 2026)

Šest původních testů T04 chybně předpokládalo, že Membership vytváří či ruší efektivní oprávnění role. Nahrazeny jsou šesti regresními testy, které porovnávají množinu `EffectiveAccess` (identita, objekt, typ role, všechny zdroje a interval platnosti) před a po vytvoření či odebrání členství. Případy zahrnují aktivní, budoucí, expirované a dvě souběžná členství a nezávislou expiraci samotné Role.

**Invariant:** `Project(Roles, MembershipsA) == Project(Roles, MembershipsB)` pro libovolnou změnu členství při ostatních nezměněných autorizačních datech. Testy nepředpokládají, že stará projekční implementace je správná; případný červený test je regresní důkaz chyby v `SecurityProjectionService`, nikoli důvod měnit očekávání.


### Stage 2 – oprava projekce Membership (fix3)

Po běhu 72/75 byly tři neúspěšné případy T04 způsobeny produkční větví `CalculateRows()`, která vytvářela oprávnění přes `Membership`. Větev i související čtení Membership při cíleném a úplném rebuild byly odstraněny. `Role`, `RbacAccessBinding`, hierarchické dědění a `Deny` zůstaly zachovány. SQLite a xUnit testy musí potvrdit 75/75; v tomto balíčku nebyly spouštěny.
