# Architecture Update 15 — Transactional security projection

## Goal

Mutations of RBAC source structures and the derived `effective_access` projection are now committed atomically.

## DBLayer as Unit of Work

`IDBLayer` now stages mutations and exposes explicit `SaveChangesAsync` plus `ExecuteInTransactionAsync`. Store implementations no longer call `SaveChangesAsync` themselves.

## Role and Group mutations

Create/update/delete of `RoleModel` and `GroupModel` follows one database transaction:

1. Stage the source mutation.
2. `SaveChangesAsync` so the projection rebuild can query the new source state.
3. Rebuild `EffectiveAccess` (stage delete + inserts).
4. `SaveChangesAsync` for the projection.
5. Commit the transaction.

Any exception rolls back both source data and `EffectiveAccess`. Multiple `SaveChangesAsync` calls intentionally occur inside the same database transaction.

## Ordinary secured entities

Generic `BaseService` mutations also use an explicit transaction and one `SaveChangesAsync`.

## Compatibility cleanup

The update also includes the previous local compilation fixes: `Microsoft.AspNetCore.Builder` for `MapGroup`, a `SecurityOperations` constants class, and compile exclusions for stale historical Base service/security files when an update is overlaid onto an older workspace.

## Projection rebuild safety

`SecurityProjectionService.RebuildAllAsync()` also opens a transaction when invoked standalone (for example during startup). `DBLayer.ExecuteInTransactionAsync` detects an already active transaction, so when called from `GroupService` or `RoleService` it participates in the outer source-mutation transaction rather than nesting a second database transaction.
