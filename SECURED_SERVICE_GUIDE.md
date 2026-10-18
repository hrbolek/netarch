# Secure-by-default services: introduction

## New entity example

```csharp
public sealed class DepartmentService : SecuredEntityService<DepartmentModel>
{
    public DepartmentService(IAuthorizationLayer security, IDBLayer db)
        : base(security, db) { }
}
```

A read DTO implements `IEntityProjection<DepartmentModel, DepartmentResponse>`
with a SQL-compatible `static Projection`; update output additionally implements
`IEntityMapper<DepartmentModel, DepartmentResponse>` with `static FromModel`.

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

All entity-service contracts inherit `ISecuredEntityService<TModel>` and
implementations derive from `SecuredEntityService<TModel>`. The former
`IBaseService` is removed. `RbacAuthorizationService` implements the independent
`IRbacAuthorizationService` permission-decision contract; it must not inherit
the entity service, which would cause recursive authorization.

Updates implement `IUpdateDTOModel<TModel>` and supply the original `updated_at`.
Do not replace that token with the current time or a freshly reread timestamp.
`ApplyTo` changes business fields only. The authorized staging pipeline owns
audit identity/timestamps and EF uses the original token in its UPDATE
predicate. HTTP callers receive 400 for missing/invalid tokens and 409 for a
stale token or a concurrent write. HTML forms include the original timestamp
as a hidden field in round-trip (`O`) format.

`VectorAsync` without an explicit limit remains unbounded;
`VectorPageAsync` defaults to 25 items and reports continuation metadata.
Do not substitute one for the other without explicitly deciding paging behavior.

Static resolver classes cannot inherit. Map standard closed generic
`BaseResolvers` methods directly, as the Core entity endpoints do; retain specialized
handlers for domain orchestration, forms and relations.
