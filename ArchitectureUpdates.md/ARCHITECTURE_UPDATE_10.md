# Architecture update 10

This update introduces the query/security boundary required for HTML/HTMX, REST and future OData/Excel access.

## Main rules

- `IDBLayer` is the central raw persistence gateway and exposes `IQueryable<TEntity>` over DB entities.
- `ISecurityLayer` is the application-facing query gateway for secured DB entities.
- `BaseService.Query()` returns `IQueryable<DTO>`, never `IQueryable<DBEntity>`.
- Every response DTO implements `static Projection` in addition to `FromModel`.
- READ filtering is applied before DTO projection.
- UPDATE and DELETE are checked against `rbac_id` in `BaseService`.
- `EffectiveAccess` is a materialized database projection used to keep READ filters SQL-composable.
- Source of truth remains Group hierarchy + Roles + RoleTypes; `EffectiveAccess` is rebuilt from them.

## Query pipeline

`ApplicationDbContext -> DBLayer.Query<TEntity>() -> SecurityLayer.Query<TEntity>() -> Select(TDTO.Projection) -> IQueryable<TDTO>`

Transport layers (HTML, REST, OData/Excel) consume DTOs only.

## EffectiveAccess

Columns:

- `user_id`
- `rbac_id`
- `role_type`
- `source_role_id`
- `source_group_id`
- `valid_from`
- `valid_to`

The projection is rebuilt at startup and after Role or Group mutations. This implementation intentionally uses a full rebuild first; it can later be optimized to subtree/role-local rebuilds without changing the service/query contracts.

## Lifetime

`ApplicationDbContext`, `DBLayer`, `SecurityLayer`, stores and services are scoped, not singleton instances. DBLayer is a singleton architectural gateway concept, but follows the EF Core request scope.
