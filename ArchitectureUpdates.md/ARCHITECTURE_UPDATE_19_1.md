# Architecture Update 19.1

## Operation catalog compatibility migration

Update 19 introduced deterministic ids for well-known RBAC operations so runtime authorization can compare `role_type_operations.operation_id` directly without joining the `operations` table.

Older databases may already contain the same operation names under randomly generated ids. `EnsureOperationCatalogAsync` now reconciles these rows transactionally:

1. find the existing operation by name,
2. temporarily rename the historical row to release the unique `operations.name`,
3. insert the well-known operation with its deterministic id,
4. repoint all `role_type_operations` rows from the historical id to the deterministic id,
5. delete the historical operation,
6. commit the transaction.

A collision where a deterministic id is already assigned to a different operation name fails explicitly rather than silently corrupting authorization data.

This makes Update 19 compatible with existing databases and avoids requiring a database reset merely because operation ids became deterministic.
