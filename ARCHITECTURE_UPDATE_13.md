# Architecture Update 13

## DB models moved to DBLayer

`DBLayer` now owns all persistence entities and store contracts:

- `UserModel`
- `GroupModel`
- `GroupTypeModel`
- `MembershipModel`
- `RoleModel`
- `RoleTypeModel`
- `OperationModel`
- `RoleTypeOperationModel`
- `EffectiveAccessModel`

Domain `*.Models` projects contain request/response DTOs only and reference `DBLayer` for their persistence model generic parameter and projection expressions.

The dependency direction is now:

```text
Base
  ↑
DBLayer
  ↑
SecurityLayer
  ↑
Services
  ↑
Resolvers / UI / OData
```

DTO projects also reference `DBLayer`, but `DBLayer` does not reference DTO/domain projects.

## Operations post-enrichment

`SecurityLayer` adds a bulk API:

```csharp
Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetAllowedOperationsAsync(
    IEnumerable<Guid> rbacIds,
    CancellationToken cancellationToken = default);
```

The implementation resolves operations for the current user in one query over:

```text
EffectiveAccess
  -> RoleTypeOperation
  -> Operation
```

`OperationsEnrichmentExtensions` provides post-query enrichment:

```csharp
await items.EnrichWithOperationsAsync(
    security,
    item => item.rbac_id,
    (item, operations) => new TargetDto(item, operations));
```

and a generic envelope:

```csharp
await items.WithOperationsAsync(security);
```

The extension materializes the source once, collects distinct `rbac_id` values, performs one bulk permission query, then maps in memory. This avoids N+1 permission queries and keeps the main `IQueryable<DTO>` SQL/OData query simple.

For `IQueryable`, apply filtering, ordering and paging first, then call `ToListWithOperationsAsync` as the terminal enrichment step.
