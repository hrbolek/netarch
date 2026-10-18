# ZArchitecture 103 — Identity hardening and PostgreSQL-only persistence

## Security

Removed the `actorUserId` query-string fallback from `HeaderOrClaimActorProvider`. The URL query string no longer selects the effective user identity.

**Security warning:** `X-Actor-UserId`, `x-ms-client-principal-id`, and `x-ms-client-principal` are still trusted as identity sources in this version. Production must only accept these when injected by a trusted reverse proxy that removes forged client headers. Future hardening should restrict the development-only `X-Actor-UserId` mechanism and prioritize authenticated claims.

## Persistence

The live Host already uses EF Core `UseNpgsql`. Removed unused legacy alternative implementations:

- `InMemoryStore<TModel>`
- `SqliteRbacService` and `AddSqliteRbac`
- SQLite-specific raw SQL `DatabaseDumpService` and its interface
- `Microsoft.Data.Sqlite` package reference in Base
- obsolete SQLite WAL comment and incorrect Host storage description

These removed APIs have no references in the current source tree, but any outside consumers must be updated. No schema migration is required.

## Tests

Added regression tests for query-string identity rejection and claim precedence. Existing test fixtures run on PostgreSQL; historic comments and changelogs may mention SQLite as prior implementation. No `.NET SDK` is available here to compile or execute the tests.
