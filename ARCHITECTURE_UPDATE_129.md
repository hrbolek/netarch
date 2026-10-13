# ZArchitecture 129 — vstupní rozcestník a UI Events

- Obnoveno `GET /` jako HTML-first `ApplicationHomePage` se seznamem registrovaných aplikací; `GET /architecture` je zachováno.
- `EventsModule.ConfigureNavigation` registruje aplikaci `Events` s root URI `/events/EventResponse/list`.
- UI host mapuje `/events/EventResponse/list` přes `ApplyViewModel<EventListPage,List<EventModel>>`; vrací HTML bez Accept a JSON při `Accept: application/json`.
- API-only host mapuje toto URI pouze na JSON a nepoužívá Razor komponentu endpointu.
- `Events.csproj` používá Razor SDK pro kompilaci `EventListPage.razor` v `src/Apps/Events`.
- Výpis událostí omezen podle RBAC pro aktuálního uživatele; chybějící identita vrací 401.
- Žádné změny v Core.

## Ověření

`dotnet build src/Apps/Host/Host.csproj` a `dotnet test SignFakeArchitecture.sln`.
.NET SDK v prostředí generování není dostupné; sestavení proto nebylo ověřeno.
