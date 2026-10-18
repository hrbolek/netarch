# Architecture Update 16

## Transaction scope and SQLite concurrency

`DBLayer` remains the owner of transaction boundaries. It exposes `HasActiveTransaction` and the two `ExecuteInTransactionAsync` helpers. A nested call never opens a second EF Core transaction for the same scoped `ApplicationDbContext`; it joins the already active transaction. Parallel HTTP requests still receive independent scoped DbContexts and therefore independent transactions.

SQLite is configured for WAL mode and a 5 second default busy timeout. This supports many concurrent readers and serializes writers without failing immediately when another request currently owns the write lock.

## EffectiveAccess invalidation

`EffectiveAccess` is rebuilt only when its derived authorization topology can change:

- `Role` create/update/delete: rebuild required.
- `Group` create/delete: rebuild required.
- `Group` update: rebuild only when `master_group_id` changes.
- ordinary Group attributes such as name or group type: no rebuild.
- `RoleTypeOperation` changes: no rebuild because operations are joined dynamically through `role_type_id`.
- ordinary secured entity CUD: no rebuild.

Source mutation and projection rebuild remain in one database transaction. The projection rebuild may call `SaveChangesAsync` more than once internally, but those flushes do not commit the outer transaction.
