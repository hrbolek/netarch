# ZArchitecture 102 — dokumentační vydání

## Rozsah a neměnnost chování

Tato verze je určena výlučně pro dokumentaci. **Není měněna implementace, signatury, routy, konfigurace, databázové schéma ani běhové chování**. Proti verzi 101 jsou v C# souborech přidané pouze XML dokumentační komentáře `///`; další změnou jsou dokumentační soubory.

Vytvořeno 241 dokumentačních bloků pro typy a 357 pro metody v 230 zdrojových souborech. Především: DBLayer modely, vstupní a výstupní DTO, Services a rozhraní služeb, Resolvers a vrstva SecurityLayer.

## Jak číst vrstvy

- **DBLayer**: databázové entity, trvalé vazby, jednotka práce a perzistence. Datový model definuje identitu a FK, ne HTTP chování.
- **SecurityLayer**: vyhodnocování přístupů podle efektivního uživatele a RBAC objektu, autorizační dotazy a projekce. Členství (`Membership`) a přiřazení role (`Role`) jsou nezávislé.
- **Services**: business operace, ověření invariantů, kontrola RBAC a transakční změny. Volání mezi službami je preferováno před přímým přístupem z resolveru do DB.
- **Resolvers**: Minimal API adaptér — mapování rout, čtení parametrů a těla požadavků, delegace do Services, výběr HTTP/JSON/Razor odpovědi.
- **UI**: Razor komponenty, zejména `LargeCard` a `MediumCard`. Obchodní stav se nevypočítává v markup. FK navigace využívá společnou `EntityLink`.

## Důležité doménové invarianty

### Digitální proces

`ProcessDefinition` vlastní právě jednu identitu `FormDefinition` a jednu `WorkflowDefinition`. `ProcessDefinitionVersion` vybírá odpovídající `FormVersion` a `WorkflowVersion` pouze z těchto definic. Běhový `Request` odkazuje na použitou verzi procesu; při publikování další verze se historické žádosti nemigrují na jinou definici.

Při založení procesu `ProcessDefinitionService.CreateTypedAsync` vytváří proces, formulář, workflow a jejich první pracovní verze **v jedné transakci**. Autorizační skupiny potomků vznikají v rámci autorizovaného scope a efektivní přístup se synchronizuje poté, co jsou záznamy uloženy.

### RBAC

- Datové řádky nesou `rbacobject_id`, který slouží jako cíl autorizace.
- Role nad RBAC objektem je nezávislá na Membership; změna členství sama neukončuje roli.
- Skupiny tvoří strom, jejich materializovaná cesta umožňuje předkovské dotazy.
- `EffectiveAccess` je odvozená projekce a není primárním zdrojem oprávnění.
- Vlastní HTTP klient ani UI nesmí určovat `rbacobject_id` nové entity bez serverové kontroly vytvářecího scope.

## Vzor čtení resolveru

Například `ProcessDefinitionResolvers.CreateApi(request, service, ct)` deleguje na `IProcessDefinitionService.CreateTypedAsync`. `CreateForm` převádí HTML pole na typovaný požadavek a po úspěchu přesměruje na detail. `Detail` ověřuje čtení rodičovské definice přes `ISecurityLayer.GetByIdAsync` a získává dostupné verze z bezpečnostně filtrovaného `IQueryable`. `Map` registruje routy a odpovědi pomocí ViewModel/Razor kontraktů.

Tento tok čti od HTTP endpointu k resolveru, od resolveru k service, od service přes SecurityLayer k DBLayer, a návrat přes response DTO do Razor komponenty. Oprávnění se nikdy nemají odvozovat pouze z toho, zda se zobrazí tlačítko v UI.

## Praktické používání XML komentářů

- V IDE použij kurzor nad modelem, službou nebo resolverem pro rychlou nápovědu.
- Při změně chování služby nejprve aktualizuj její XML `summary`, zejména podmínky RBAC a transakční hranice.
- U API endpointů udržuj současně `.WithDescription(...)` pro Swagger: XML dokumentace je určena programátorům, `.WithDescription` konzumentům HTTP API.
- Nepřenášej detaily SQL/EF do resolveru ani detaily HTTP do DBLayer.

## Validace

Porovnání textu C# souborů s verzí 101 potvrdilo, že všechny změny jsou vložené řádky XML komentářů a nedošlo k přepsání nebo odebrání zdrojových řádků. Build a testy nebylo možné spustit, protože v prostředí není `dotnet` CLI. Doporučeno: `dotnet build SignFakeArchitecture.sln` a `dotnet test SignFakeArchitecture.sln`.
