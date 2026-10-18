# Events: domain extension example

Events is an example of adding a domain without putting persistence or authorization
logic in HTTP endpoints, and without putting HTML in resolvers.

## Project boundaries

```text
Events.Models
    └── entity contracts and EF model configuration
Events.Services
    └── secured domain operations, tree behavior, form/import orchestration
Events.UI
    └── Razor pages, HTML renderers, CSS and JavaScript assets
Events.Resolvers
    └── HTTP routing, actor checks, service orchestration and response shaping
EventsModule.cs
    └── dependency injection, database registration, navigation and route mapping
```

The direction of the main project references is:

```text
Models <- Services <- UI <- Resolvers
                    ^       ^
                    └───────┘
```

`Events.UI` can use service contracts to render a view, but `Events.Services` and
`Events.Models` must not depend on the UI or resolver projects. Resolvers should
not inject a `DbContext`, `IDBLayer`, `IAuthorizationLayer` or raw RBAC service.
Register new services in `EventsModule`, map UI and API routes in
`EventsEndpointExtensions`, and keep the route contract stable when changing an
implementation.

## Authorization and data access

- `EventService` is a `TreeSecuredService<EventModel>` and `EventTypeService` is
  a `SecuredEntityService<EventTypeModel>`. Normal reads use their secured,
  operation-specific query sources; normal writes use authorization-layer
  staging and transactions.
- Tree creates and moves must use the tree staging helpers so the persisted
  materialized paths and descendant paths stay consistent with `ParentId`.
  A move requires update access to the destination parent and retains the
  entity's existing RBAC scope.
- Resolver actor checks reject anonymous requests. They are not substitutes for
  service-level RBAC checks; services still enforce the operation on the data.
- Unfiltered reads are limited to domain invariants and commands whose policy
  is distinct from RBAC: global type-code uniqueness, hierarchy/cycle checks,
  relationship existence, and registration-policy or duplicate-participant
  checks after the relevant event operation has been authorized.
- Self-registration and accepting/declining one's own invitation are deliberate
  self-service commands. They constrain records to the trusted current actor
  and enforce registration policy/status rules; they do not grant a general
  read or write path for event or participant data.
- Bulk imports write batches through `CoreDbContext` to preserve their import
  transaction and deduplication behavior. They independently require `Create`
  on the selected RBAC scope before reading or writing import data. Keep these
  paths confined to the import services; do not use them for ordinary CRUD.

These domain-specific commands are explicit exceptions, not alternate general
query APIs. A new ordinary endpoint should call the secured domain service and
must not query or mutate the context directly.

## Presentation

Razor pages and the timetable HTML renderer live in `Events.UI`. UI assets are
owned and embedded by that project, while `EventsUiAssets` preserves the public
`/events/events-assets/{file}` URLs. Resolvers choose a representation and
delegate rendering; they do not construct HTML or own presentation resources.

## Adding another domain

1. Define the entity and its EF configuration in a `*.Models` project.
2. Implement ordinary read/write operations in a secured service. For tree
   entities, derive from `TreeSecuredService<T>` and use its path-aware staging
   operations.
3. Put Razor pages and visual renderers/assets in `*.UI`.
4. Keep resolvers thin: resolve the trusted actor, invoke services, and shape
   HTTP responses. Do not inject persistence or raw authorization services
   into endpoint handlers.
5. Register the domain services, database model configuration, navigation,
   and API/UI routes in the module.
6. Keep any necessary self-service or bulk-import exception narrowly scoped,
   actor/scope-bound, independently authorized, and documented alongside the
   service method.
