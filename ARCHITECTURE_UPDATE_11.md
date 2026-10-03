# Architecture update 11

Two infrastructure libraries are now explicit:

- **DBLayer** — raw persistence, EF Core, `ApplicationDbContext`, stores, materialized `EffectiveAccess`, generic raw `IQueryable<TEntity>`.
- **SecurityLayer** — current-user authorization, secured query composition, secured CRUD facade, `SecurityService`, and rebuilding of security projections.

The dependency direction is:

`Resolvers / UI / OData -> Domain Services -> SecurityLayer -> DBLayer -> ApplicationDbContext -> SQLite`

Important invariants:

1. Domain Services never inject `IDBLayer`, `DbContext`, or entity Stores.
2. Domain Services access secured DB entities only through `ISecurityLayer`.
3. `ISecurityLayer.Query<TEntity>()` applies the RBAC filter before DTO projection.
4. Services return DTOs or `IQueryable<DTO>`, never DB entities.
5. DBLayer may use specialized Stores internally (for example Group materialized-path maintenance), but those stores are not application-facing.
6. DBLayer and SecurityLayer are both scoped at runtime; they are singleton architectural layers, not DI singletons.
