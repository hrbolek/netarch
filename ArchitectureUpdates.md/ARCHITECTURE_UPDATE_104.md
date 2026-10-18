# ZArchitecture 104 — secured generic services by composition

- Renamed generic service to `SecuredBaseService<TDBModel,TDTOModel>`; standard CRUD remains secured.
- Introduced internal `DataOperations<TDBModel>` composed in the constructor with the scoped `IDBLayer`, **not** independently registered in DI.
- Added `protected Unsecured_Query`, `Unsecured_ReadAsync`, and raw stage create/update/delete. These bypass RBAC and must not be exposed to an HTTP API or standard service interface.
- Added `ISecurityQueryProvider`, with `RbacSecurityQueryProvider` preserving the existing server-side EffectiveAccess SQL filtering. Other policies can replace this adapter in a later iteration; CUD still goes through existing secured `ISecurityLayer` logic.
- Updated concrete services to inherit the secured base and receive the scoped DB gateway in their constructors.
- Kept `IBaseService` stable and unchanged. No schema changes.

## Security constraints

Existing secured `ISecurityLayer` methods continue to enforce CUD authorization, delegation and RBAC scope moves. The raw DB adapter is deliberately scoped to protected domain use only. `protected` is a programming boundary, not a privilege escalation control: inherited services must not leak raw results.

## Build status

No dotnet SDK is available here. Requires compilation and PostgreSQL integration tests before production adoption.
