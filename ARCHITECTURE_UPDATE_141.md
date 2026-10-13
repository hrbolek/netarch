# ARCHITECTURE_UPDATE_141 — Events layered refactor following /templates/

## Scope

Refactors the **existing** `src/Apps/Events` implementation in the complete ZArchitecture solution, preserving existing `events` tables, data, import logic, URLs and module registration. Based on `/templates/AGENTS.md`, `DOMAIN_CONTRACT.md`, `ENTITY_CONTRACT.md`, `ENDPOINT_CONTRACT.md`, `UI_COMPOSITION_CONTRACT.md` and `INPUT_COMPONENTS_CONTRACT.md` (version 140).

## Layer boundaries

- `Events.Models/Events.Models.csproj`: secured EF entity models, EF mappings, fixed event-type definitions.
- `Events.Services/Events.Services.csproj`: domain service, input/output records, form-option provider, import services, read-only lookup filtering.
- `Events.Resolvers/Events.Resolvers.csproj`: Minimal API registration, HTTP handlers, existing specialized timetable/academic-plan HTTP representations.
- `Events.UI/Events.UI.csproj`: Razor pages, domain cards, navigation.
- `Events.csproj`: thin application-module adapter and existing database bootstrap hosted service.

Uses the same namespace (`WebAppArchitecture.Events`) to preserve existing contracts across the refactoring. Project dependencies follow models -> services -> UI/resolvers -> module, with no service reference to UI/resolvers.

## HTTP/OpenAPI

- Moves endpoint registration out of `EventsModule` into `EventsEndpointExtensions`.
- Adds explicit stable `.WithName(...)` and `.WithDescription(...)` to the 32 existing route declarations and group `.WithTags("Events")`.
- Adds `ApplyJsonModel<T>` to the JSON-first event and event-type creation endpoints; preserves existing `ApplyViewModel` for HTML-capable UI endpoints.
- Preserves API-only and UI modes and their legacy paths; names use separate suffixes for list API vs list page variants.
- No additional fake CRUD endpoints have been registered where service/UI implementation is absent.

## Razor forms / foreign keys

- Reuses **actual** shared `EntityLookupSelect` (Core/Base) for event type, event parent and type parent selection, on **Create and Update**.
- Pre-populates selections with `InitialId` / `InitialLabel` when editing.
- Reuses existing list endpoints for lookups; bounded `_ilike` filter extraction supports `name` and `code` as requested by the shared Core lookup javascript. It runs on the RBAC-visible records.
- Scalar inputs, save handlers, and specialized HTML planning views retain their existing behavior; further componentization is still desirable.

## Deviations from the final template contract / follow-ups

1. This is a compatibility-preserving refactor rather than a rewrite of domain service inheritance and DTO generics. `EventService` still exposes custom methods and not the full canonical generic service/API operation matrix.
2. Timetable and academic plan still use their legacy HTML/JavaScript page renderers, not dedicated Razor visualization components.
3. Basic scalar inputs and browser POST handling are still custom; `EntityLookupSelect` was adopted for FK fields only.
4. Some read-heavy endpoints still query `CoreDbContext` from HTTP handlers, and collection authorization currently iterates rather than performing batched permission projection. These are **known violations** of the ideal service/resolver boundary and should be addressed in another dedicated step without changing behavior inadvertently.
5. Existing data, DB bootstrap, and independent membership/role RBAC behavior are unchanged.

## Verification

Performed: static project-path/reference checks, required source-file checks, route-name uniqueness checks, and ZIP integrity checks.

Not performed: actual `dotnet build`, generated OpenAPI verification, Razor compilation, integration testing or PostgreSQL runtime checks. The SDK is not available in this execution environment. Do **not** treat this build as verified for deployment without these tests.
