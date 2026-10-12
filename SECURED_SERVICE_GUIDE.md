# Secure-by-default services: introduction

## New entity example

```csharp
public sealed class DepartmentService : SecuredBaseService<DepartmentModel>
{
    public DepartmentService(ISecurityLayer security, IDBLayer db)
        : base(security, db) { }
}
```

A response DTO implements `IEntityDTOModel<DepartmentModel, DepartmentResponse>`
and provides both `static FromModel` and a SQL-compatible `static Projection`.

```csharp
var summaries = service.Query<DepartmentSummaryResponse>();
var detail = await service.GetByIdAsync<DepartmentDetailResponse>(id);
```

The type argument on the service is the **DB model only**. A method chooses
its DTO. `Query<TDTO>()` always applies RBAC **before** projection.

## Relational projections

For public/reference data, navigation properties may be projected using a
SQL-translatable `Expression`, with no `Include` required:

```csharp
Expression<Func<DepartmentModel, DepartmentResponse>> projection =
    department => new DepartmentResponse(
        department.id, department.name, department.parent!.name);
```

But using `department.parent.name` alone does not enforce the parent's own RBAC.
For independently secured sources, compose two `SecuredModels()`/
`SecuredRelated<T>()` queries, join them while still `IQueryable`, and then
project the result. Both query roots must use the same EF DbContext. For
complex collections, batch-load related IDs and enrich DTOs after both sides
have been authorized; avoid an N+1 pattern.

## Privileged operations

Use `Unsecured_Query()` and other `Unsecured_*` methods only for internal
invariants and with a documented justification. They must never be exposed by
HTTP resolvers. `ISecuredEntityService<T>` never exposes raw DB entities.

## Migration

Existing concrete services continue to derive from the deprecated two-parameter
adapter until their resolver and service interfaces are migrated. New services
can immediately use the one-parameter implementation.
