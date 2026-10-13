# ARCHITECTURE_UPDATE_131 – Events: creation UI and empty database onboarding

- Added `/events/EventResponse/new` as a Razor page for creating the first event, with dates, event type, parent, description and RBAC scope.
- Added `/events/EventTypeResponse/new` as a Razor page to initialize event types without relying on Swagger or hand-crafted JSON.
- Both forms POST JSON to the existing domain Minimal API operations; authorization remains on `EventService.CreateAsync` and `CreateTypeAsync`.
- Scope choices come only from Groups for which the current actor has Core RBAC `create` permission. No direct role-based privilege granted by event participant roles.
- Added navigation links and empty-state guidance to Events list.
- Host/Core unchanged; new files are under `src/Apps/Events`.

**Validation:** inspected source and archive contents; .NET SDK is unavailable here, so compilation and runtime tests must be run by the project owner.
