# Architecture Update 19

## Security query optimization

`SecurityLayer.Query<TEntity>()` now uses a correlated `EXISTS`-style LINQ predicate instead of `JOIN + DISTINCT`.

The runtime authorization path no longer joins `operations`. Well-known RBAC operations have stable GUIDs (`RbacOperationIds`) and the initializer seeds the `operations` table with those deterministic IDs.

Target SQL shape:

```sql
SELECT ...
FROM entity e
WHERE EXISTS (
  SELECT 1
  FROM effective_access a
  WHERE a.rbac_id = e.rbac_id
    AND a.user_id = @userId
    AND a.valid_from_unix_ms <= @now
    AND (a.valid_to_unix_ms IS NULL OR a.valid_to_unix_ms >= @now)
    AND EXISTS (
      SELECT 1
      FROM role_type_operations rto
      WHERE rto.role_type_id = a.role_type_id
        AND rto.operation_id = @operationId
    )
)
```

This removes duplicate entity rows, `DISTINCT`, the wrapper subquery produced by EF Core, and the runtime join to `operations`.

Because operation IDs changed from random bootstrap GUIDs to deterministic well-known GUIDs, recreate the prototype SQLite database when moving from Update 18.
