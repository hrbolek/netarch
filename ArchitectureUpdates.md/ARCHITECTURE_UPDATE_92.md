# ZArchitecture 92 — oprava kompilace a administrace RBAC rolí

## Změny

- Doplněn `using Microsoft.Extensions.Logging;` do `FormDefinitionResolvers.cs`, čímž se řeší `CS0246` pro `ILoggerFactory`.
- Existující sdílená komponenta `RbacPermissionList.razor` doplňuje pro role přiřazené **přímo** zobrazované RBAC skupině navigaci k editaci role/platnosti a k detailu role s dostupnými akcemi (včetně existujícího autorizovaného odebrání).
- U zděděných rolí se nezobrazuje zavádějící editace na aktuální skupině, ale upozornění na zdrojovou skupinu.
- Stránka `/architecture/root-permissions` obsahuje vysvětlení úpravy role, časového omezení a rozdílu přímých/zděděných přiřazení.

## Bezpečnost

Využity existující role endpoints. Žádné obcházení RBAC, žádné přímé volání DB z UI. Server stále kontroluje Update/Delete nad rolí. Tlačítka jsou navigací; zobrazení odkazu samo právo nezaručuje.

## Ověření

Dotnet SDK není v pracovním prostředí dostupné; kompilaci a testy musí ověřit `dotnet test SignFakeArchitecture.sln -c Debug`. Archiv ověřen testem integrity ZIP. Žádná migrace databáze.
