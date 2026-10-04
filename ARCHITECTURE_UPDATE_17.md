# Architecture Update 17 — query-integrated authorization

This update removes the historical per-row authorization pass from list/vector resolver paths.

## Core rule

Authorization required to return an entity is part of the database query itself:

- `GetByIdAsync` -> `read`
- `GetAllAsync` / `Query()` -> `list`
- default `VectorAsync(predicate)` -> `list`
- `VectorAsync(predicate, operation)` -> caller-selected operation (for example `vector`)

`SecurityLayer.Query<TEntity>(operation)` remains the single row-level query filter.

## Removed N+1 paths

`BaseResolvers.List` no longer calls `FilterAuthorizedAsync` for each DTO.

Group/User relation endpoints no longer re-check every returned role/membership one by one. Their `VectorAsync` already applies `list` in SQL.

`UserResponse/vector` no longer calls authorization once per result. It calls:

```csharp
service.VectorAsync(predicate, RbacOperations.Vector, cancellationToken)
```

so the `vector` permission is part of the original SQL query.

## Operations for UI

Allowed-operation enumeration is deliberately separate from authorization. `GetAllowedOperationsAsync(IEnumerable<Guid>)` and `OperationsEnrichmentExtensions` remain available as an explicit batch facility for UI metadata. They are not automatically invoked by list endpoints.

Thus `/architecture/GroupResponse/list` should normally execute one data/security query, not one query plus one permission query per returned group.
