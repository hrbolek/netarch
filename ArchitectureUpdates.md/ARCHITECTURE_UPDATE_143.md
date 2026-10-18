# ZArchitecture 143 — akademický plánovač v Razor

## Nové chování
- Akademický harmonogram z `excel.babel(1).html` převeden do Razor komponenty `AcademicPlannerPage`.
- ISO týdenní hlavička; každá buňka má dvojici trojúhelníkových polovin Po–St a Čt–Ne.
- Barevná paleta typů a zkratky v kalendáři, podporován horizontální posuvník.
- Přepínání intervalů, rozdělení stávajících úseků, slučování sousedních úseků stejného typu, zachování UUID tam, kde je to možné.
- Přehled změn added/updated/deleted, reset a uložení pomocí existujícího PUT `/events/EventResponse/academic-plan/{id}`.
- Razor vykresluje celou základní strukturu a CSS. Samostatný malý JS je nutný pro interaktivitu, protože stránky jsou renderované jako statický HTML fragment přes `ApplyViewModel`, bez aktivního Blazor circuit.
- Stávající GET `/events/EventResponse/academic-plan/{id}` zachovává JSON při Accept application/json; požadavek na HTML přesměruje na novou Razor stránku `/events/EventResponse/academic-plan/editor/{id}`.
- Výsledky a uložení stále používají stávající service / zabezpečení / optimistickou kontrolu `ExpectedUpdatedAt`.
- CSS a JS jsou vloženy jako embedded resources v Events.Resolvers, endpoint `/events/events-assets/{file}` (nevyžaduje kopírování statických souborů Hostem).
- React, Babel a externí CDN nejsou potřebné; data se načítají z databáze, nikoli z demo polí React souboru.

## Součásti
- Events.UI/AcademicPlannerModel.cs
- Events.UI/AcademicPlannerPage.razor
- Events.Resolvers/wwwroot/events-assets/academic-planner.css
- Events.Resolvers/wwwroot/events-assets/academic-planner.js
- Events.Resolvers/EventsEndpointExtensions.cs
- Events.Resolvers/Events.Resolvers.csproj

## Omezení / ověření
- Dlouhý plán je omezen pro render na 120 ISO týdnů.
- Typy událostí zobrazí podle RBAC; editovatelnost podle update práva na rodičovskou událost; server při uložení kontroluje i jednotlivé úseky.
- Dny jsou v ukázce interpretovány jako kalendářní data od/do, stávající API však ukládá DateTimeOffset UTC. Pro produkční období kolem DST je vhodné potvrdit model kalendářního dne.
- SDK dotnet v prostředí není k dispozici, nebyl proveden build ani browser E2E test.
