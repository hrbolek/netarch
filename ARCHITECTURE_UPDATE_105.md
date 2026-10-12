# ZArchitecture 105 — DTO-independent secured entity services

## Architecture

New **`SecuredBaseService<TDBModel>`** (single type argument) implements the new
`ISecuredEntityService<TDBModel>` contract. DB access stays in private
`DataOperations<TDBModel>`, instantiated by the secured service; it is not DI-registered.
The public API exposes only approved `IEntityDTOModel<TDBModel,TDTO>` projections:

- `Query<TDTO>()` — RBAC-secured, SQL-translatable projection (`TDTO.Projection`).
- `GetByIdAsync<TDTO>(id)` and `GetAllAsync<TDTO>()`.
- `Where<TDTO>(predicate, operation)` — SQL composed after RBAC filtering.
- `UpdateAsync<TUpdate,TDTO>(request)` — update authorization and transactional write.
- `DeleteAsync(id)` — existing operation-specific authorization.

Privileged `Unsecured_*` methods stay `protected`; `SecuredModels` and
`SecuredRelated<TRelated>` enable careful internal composition of one or two
independently authorized SQL sources **in a shared EF context**.

## Staged compatibility

The existing `SecuredBaseService<TDBModel,TDTOModel>` now derives from the
single-generic implementation as an **obsolete compatibility adapter**.
Existing `IBaseService<TDBModel,TDTOModel>`, resolver contracts, and concrete
services still work through the existing signatures; their full migration is a
separate step, so no mass change to 21 domains was risked without a build.

## Security constraints

A projected navigation property does **not** automatically check authorization
of a separately secured related entity. Where necessary, use `SecuredRelated<T>`
with a join, or batch enrichment after filtering both sources. Neither raw
model `IQueryable` nor unrestricted projection lambdas are exposed in the new
public interface. Projection is restricted to DTOs implementing the typed
static `Projection` contract.

## Test status

Source-level review only: .NET SDK is unavailable here. Run full build and
PostgreSQL integration tests, particularly the legacy resolver adapter and
model update methods, before deploying.
