# Architecture Update 18

## Direct entity-to-security JOIN

`SecurityLayer.Query<TEntity>()` no longer precomputes a distinct set of allowed
`rbac_id` values and filters entities with `IN (subquery)`.

The secured entity is now the outer source of the query and is joined directly
with `effective_access` by `entity.rbac_id == access.rbac_id`, followed by the
role-type/operation joins and the current-user/validity/operation predicates.

Conceptually:

```text
secured entity
  JOIN effective_access       ON entity.rbac_id = access.rbac_id
  JOIN role_type_operations   ON access.role_type_id = mapping.role_type_id
  JOIN operations             ON mapping.operation_id = operation.id
 WHERE access.user_id = current user
   AND access is currently valid
   AND operation.name = requested operation
```

`Distinct()` remains necessary because multiple effective roles may grant the
same operation for the same secured entity.

## Indexes

A new `effective_access` index is defined in the entity-join order:

```text
(rbac_id, user_id, valid_from_unix_ms, valid_to_unix_ms, role_type_id)
```

The existing user-first index is retained because it is useful for batch UI
operation enrichment and other user-centric security queries.

Because the EF model changed, an existing SQLite database created through
`EnsureCreated()` must be recreated for the new index to be physically added.
