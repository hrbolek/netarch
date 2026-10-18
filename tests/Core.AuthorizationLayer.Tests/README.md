# Core – bezpečnostní testovací sada ZArchitecture 78

## Účel

Tento projekt je první skutečně implementovanou částí **Etapy 1** bezpečnostní testovací strategie. Testuje přímo EF Core SQLite, skutečný `SecurityProjectionService`, `RbacAuthorizationService` a `AuthorizationLayer.Query<T>()`. `EffectiveAccess` není nahrazován mockem. Testy běží ve vlastních izolovaných databázích se zapnutými cizími klíči.

**Bezpečnostní kontrakt:** Po přidání nebo odebrání role, bindingu nebo `RbacDenyRule` se musí účinná projekce a zabezpečené SQL shodovat. Deny na RBAC sekci nesmí odstranit oprávnění nesouvisející role, ani změnit sousední podstrom.

## Spuštění

Z kořene repozitáře:

```bash
dotnet restore tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj
dotnet test tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj -c Debug --logger 'console;verbosity=normal'
```

Pro jeden blok:

```bash
dotnet test tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj --filter 'FullyQualifiedName~T08'
```

Pro opakované lokální testy bez opětovné kompilace lze použít `--no-build`.

## Testovací data a zásady

`RbacFixture` vytváří skutečnou relační databázi SQLite `:memory:` s jedním otevřeným připojením pro dobu testu. Obsahuje strom `University → Faculty → Request → (Visible, Secret → SecretChild, Sibling)` a samostatnou `OtherFaculty`. Typy rolí jsou `Applicant`, `Dean`, `Reader`. Uživatelé `Student` a `Dean` mají jednoznačné UUID. Pro každý test je instance fixture nová a vzájemná izolace je úplná. Časy rolí jsou nastaveny relativně k aktuálnímu času a nevyžadují `Thread.Sleep`.

### T02 – RBAC hierarchie

Testy role u kořene a u Requestu ověřují směrování dědění, nepropagování do předků, izolaci jiné fakulty a přerušení `inherit_roles=false`. Pozitivní lokální role pod přerušeným děděním musí zůstat platná. Další plánované testy: změna rodiče, kontrola cyklu, mazání podstromu, materializovaná cesta po přesunu.

### T03 – RoleType / operace

`T03_And_T09_SecurityService_RespectsOperations` porovnává `Read` a `Update` na identickém RBAC cíli. `T03_RemoveRoleOperation_RevokesReadWithoutProjectionRebuild` ověřuje, že odebrání přiřazené operace okamžitě zruší `Read`, aniž by bylo nutné přepočítat samotné role. Další plán: API pro správu typů rolí, duplicity, všechny CRUD operace.

### T06 – RbacAccessBinding

Testy ověřují převod funkční role `Applicant` na operační roli `Reader`, izolovaný cílový RBAC objekt a účinek zákazu na zdrojovou roli i v případě, kdy se výsledný typ role liší. Další plán: platnost bindingu, vícenásobné zdroje a odebrání bindingu.

### T07 – EffectiveAccess

Testy ověřují odstranění posledního zdroje, přežití oprávnění při odstranění pouze jednoho zdroje, expirovanou roli, idempotenci rebuild a shodu inkrementální synchronizace s plným přepočtem po vložení/odebrání Deny. Rozdíl mezi cílenou projekcí a plným rebuildem je **bezpečnostní chyba**, nikoliv pouze chyba cache.

### T08 – RbacDenyRule

Testy ověřují přesný a rekurzivní zákaz, zachování oprávnění v sourozenecké sekci, ponechání oprávnění děkana, lokální přidělení role, odstranění zákazu a vytvoření nového potomka. **Důležité:** v aktuálním modelu je `Deny` selektivní vůči `role_type_id`, nikoliv absolutní zákaz čtení všemi rolemi. Uživatel s jinou nezakázanou rolí může i nadále získat přístup; toto chování je výslovně testováno.

### T09 – AuthorizationLayer.Query<T>

Test používá reálnou identitu v `HttpContext`, reálnou bezpečnostní službu a EF Core SQLite. `Query<GroupModel>(Read)` musí v jediném překládaném SQL dotazu vrátit pouze povolené skupiny, a to také při `Count`. Chybějící identita se odmítá. Žádný bezpečnostní filtr se nesmí provádět jen v HTML nebo rekonstrukci JSON.

## Důležité invarianty

1. **Rebuild ekvivalence:** `IncrementalProjection(D) = FullRebuild(D)` pro efektivně platné řádky (nikoliv nahodilé pořadí).
2. **Izolace:** zákaz Applicant na `Secret` neovlivní děkana ani `Visible` / `Sibling` / `OtherFaculty`.
3. **SQL konzistence:** `RbacAuthorizationService.IsAllowedAsync` a `AuthorizationLayer.Query<T>` musí pro stejný objekt, operaci a uživatele odpovídat.
4. **Fail closed:** chybějící identita ani vypršelé oprávnění nezakládají přístup.
5. **Žádné N+1:** autorizační predikát musí být vyhodnocen v databázi.

## Stav pokrytí a další práce

Tento repozitář obsahuje **automatizované integrační scénáře** pro všech šest prioritních bloků T02, T03, T06, T07, T08 a T09. **Neznamená to dosud všech původně plánovaných 86 scénářů Etapy 1.** Zbývající scénáře, včetně reparentingu, úplných kombinací časové platnosti, chybových transakcí a ověření SQL plánu, jsou explicitně uvedeny v kořenovém `tests/README.md` jako backlog. Nebyl zde dostupný .NET SDK, proto nebylo možné testy skutečně spustit; výsledek nesmí být interpretován jako zelená CI.

## Rozšíření Etapy 1 – sada AdditionalAuthorizationTests (ZArchitecture 78)

Soubor `AdditionalAuthorizationTests.cs` přidává 20 nových testovacích metod (23 běhových případů díky parametrizované matici čtyř operací). Testy používají stejný izolovaný `RbacFixture` a skutečný SQLite provider. Žádné autorizační rozhodnutí není mockováno.

| Blok | Nové scénáře | Bezpečnostní očekávání |
|---|---|---|
| T02 | Izolace sousední fakulty, nový vnuk RBAC uzlu, obnovení dědění | Role nepřechází do jiné fakulty; nový potomek získá správnou projekci; obnovení dědění obnoví práva |
| T03 | Matice `read/update/create/delete`, odebrání pouze `update` | Každá operace se povoluje zvlášť; odebrání `update` nesmí změnit `read` |
| T06 | Odstranění bindingu, chybějící zdrojová role, nezávislé Deny | Binding nevytvoří právo bez zdrojové role; jeho smazání odebere cílové oprávnění |
| T07 | Inkrementální versus úplný rebuild po vytvoření a odebrání role, duplicitní cíle, nový chráněný potomek | Aktivní projekce (včetně identit zdrojů) je shodná při obou způsobech synchronizace |
| T08 | Deny obecné role Dean, nezávislé zákazy, zákaz přidaný dodatečně, dvě role stejného uživatele | Zákaz se vztahuje pouze na určený typ role a správný podstrom; ostatní role i sourozenci zůstávají beze změny |
| T09 | `Where` a `Count`, `SingleOrDefault`, zakázaná operace, izolace uživatelů | Neautorizované řádky nelze vrátit ani pomocí dalšího LINQ filtru nebo dotazu na ID |

### Konkrétní testovací invarianty

- **Izolace fakult:** `Dean@Faculty` se neprojeví na `OtherFaculty`, ani na nadřazené `Root`.
- **Nová položka stromu:** přidaný potomek musí získat pravidla předků po cílené synchronizaci; rekurzivní `Deny` zůstane aktivní.
- **Operace:** dvě `RoleTypeOperation` pro `read` a `update` nezakládají práva k `create` ani `delete`.
- **Binding:** zrušení mapování `Applicant → Reader` zneplatní cílové oprávnění bez změny role Applicant u Requestu.
- **Idempotence:** duplicitní `rbacobject_id` v synchronizaci nezmění aktivní projekci.
- **Deny:** zamítnutí role Dean neblokuje Applicant a naopak; dvě nezávislá pravidla mohou být spravována samostatně.
- **SQL filtr:** dodatečný predikát typu `id == Secret || id == Visible` nemůže zpřístupnit `Secret`; stejně se chová `Count` a `SingleOrDefault`.
- **Identita:** uživatel bez explicitně přidělené role si nesmí vypůjčit přístup od jiného uživatele.

### Otevřené části Etapy 1

Toto rozšíření není náhradou za původní backlog. Zbývá zejména testování přesunu stromu a cyklů pomocí produkční služby GroupService, časové platnosti a závodů v bindingu, transakčních chyb, SQL interceptoru pro počet dotazů, komplexních vícezdrojových projekcí a kompletní API správy práv. Tyto položky nemají být označeny za dokončené bez příslušných testů.

**Stav ověření:** statická kontrola zdrojového kódu a obsahu archivu; .NET SDK není v tomto prostředí instalováno. `dotnet test` musí proběhnout v CI/devcontaineru před přijetím změn.


### T09 – fix5: RBAC context of test groups

`RbacFixture.AddGroup` assigns `rbacobject_id = id` to every `GroupModel`, including the root. Previously, all groups had `rbacobject_id = Root`, so `AuthorizationLayer.Query<GroupModel>()` checked permissions on the root for every row and returned an empty set even though `EffectiveAccess` correctly contained permissions on `Request`, `Visible` and `Sibling`. The five failing T09 tests were false negatives caused by incorrect fixture data, not by the security predicate. The assertions remain unchanged.


## Etapa 2 – rozšíření

`Stage2LifecycleTests.cs` dokumentuje v XML komentářích sady T04, T05, T10 a T12. Detailní bezpečnostní očekávání a otevřené mezery jsou v `../README.md`. Nové testy vždy vytvářejí izolovanou SQLite databázi.


## Stage 2 / T04 – Membership nesmí ovlivnit Role

Opraveno šest testů T04 v `Stage2LifecycleTests.cs`. Membership je pouze členství ve skupině, nikoli zdroj, podmínka či časové omezení role. Testy kontrolují nezměněný sémantický snímek `EffectiveAccess` při vytvoření i odstranění Membership a zachování samostatné platnosti Role. Projekční služba již z Membership nevytváří řádky `EffectiveAccess`; `source_membership_id` zůstává v databázovém modelu pouze kvůli kompatibilitě historického schématu.


### Stage 2 – oprava projekce Membership (fix3)

Po běhu 72/75 byly tři neúspěšné případy T04 způsobeny produkční větví `CalculateRows()`, která vytvářela oprávnění přes `Membership`. Větev i související čtení Membership při cíleném a úplném rebuild byly odstraněny. `Role`, `RbacAccessBinding`, hierarchické dědění a `Deny` zůstaly zachovány. SQLite a xUnit testy musí potvrdit 75/75; v tomto balíčku nebyly spouštěny.

### Etapa 2 – completion

`Stage2CompletionTests.cs` ověřuje zákaz vytvoření v cizím RBAC kontextu,
kontrolu původního i cílového kontextu při přesunu entity, kontrolu původu
zastupující role, časové meze zástupu a rollback role společně s projekcí.
Negativní testy kontrolují také nepřítomnost nepovoleného zápisu v DB.
Testy souběhu přes nezávislá SQLite spojení zůstávají samostatným úkolem;
test na jednu transakci není důkazem bezpečného paralelního provozu.


## Stage 4 / T14 — large permissions & provider-neutral instrumentation

`Stage4LargeAccessTests.cs` checks one EF database command per secure list/count,
model-level `EffectiveAccess`/`RoleTypeOperation` indexes, and an opt-in
**1500-user stress fixture** with 1500 direct roles, inherited scopes and
materialized access rows. Membership is intentionally not a source of rights.

Normal: `dotnet test tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj -c Debug`

Stress: `STAGE4_STRESS=1 dotnet test tests/Core.AuthorizationLayer.Tests/Core.AuthorizationLayer.Tests.csproj -c Release --filter FullyQualifiedName~T14_LargePopulation --logger 'console;verbosity=normal'`

The stress case measures full rebuild, single-user secure read, and incremental
resynchronization separately; timings are diagnostic, not pass/fail limits.
Do not interpret SQLite timings as PostgreSQL/SQL Server performance. Cross-provider
validation is deferred to Stage 99. This scenario does not import the production
1500-user JSON seed; it creates controlled synthetic data so that counts are known.
