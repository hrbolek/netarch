# Architecture Update 16.4

## EF Core / SQLite security query translation

`SecurityLayer.Query<TEntity>()` no longer uses nested correlated `Any()` expressions over
`EffectiveAccess -> RoleTypeOperation -> Operation`. The SQLite EF Core provider could not
translate that expression reliably when it referenced `entity.rbac_id` from the outer query.

The security filter now builds a composable `IQueryable<Guid>` of allowed RBAC ids using
explicit joins and applies it to the entity query using `Contains`:

```text
EffectiveAccess
  JOIN RoleTypeOperation
  JOIN Operation
      -> allowed rbac ids
      -> entity.rbac_id IN (subquery)
```

The query remains deferred and server-side, so it is still suitable as the secured base query
for DTO projection, HTML, REST and future OData composition.
