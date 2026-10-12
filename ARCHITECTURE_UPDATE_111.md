# ZArchitecture 111 — Per-method DTO generics

## Changes

- Removed the two-parameter `SecuredBaseService<TDBModel,TDTOModel>` compatibility class.
- All concrete Core and DigitalProcesses services inherit `SecuredBaseService<TDBModel>`.
- Changed all domain service interfaces to `IBaseService<TDBModel>`.
- Moved DTO generic selection to `Query<TDTO>`, `GetByIdAsync<TDTO>`, `GetAllAsync<TDTO>`, `VectorAsync<TDTO>`, `VectorPageAsync<TDTO>`, and `UpdateAsync<TUpdateRequest,TDTO>`.
- Preserved the mandatory compile-time constraint `where TDTO : IEntityDTOModel<TDBModel,TDTO>`.
- Updated Minimal API resolver method calls, domain resolver direct calls, and development integration adapter to supply explicit DTO generic arguments.
- Migrated Group/Role/Membership custom Update overrides to generic result DTOs without dropping transactional RBAC projection synchronization.
- Migrated Role/Membership enriched vector paging to generic overrides, retaining the existing join-based security checks for the standard detailed response DTOs.
- Updated service template and current README examples.

## Important

- No `[Obsolete]` suppression was used. The two-parameter service class was removed.
- The DTO contract itself (`IEntityDTOModel`) has **not yet been split** into separate projection/mapping/create interfaces; it still provides the compile-time model binding and both projection and mapping operations. That is a separate refinement.
- For Role/Membership vector operations using DTOs other than their standard response types, the generic secure base projection is used, not the enriched result. Audit each new projection for independent relation permissions before exposing sensitive fields.
- The source was statically checked but **not compiled or integration-tested**: the environment lacks the .NET SDK. Build/test results remain unverified.

## Verification suggested

```sh
dotnet restore
dotnet build --no-restore
```

Then run the Core and DigitalProcesses integration tests against PostgreSQL, including Role/Membership/Group update-side effect tests and RBAC denial tests.
