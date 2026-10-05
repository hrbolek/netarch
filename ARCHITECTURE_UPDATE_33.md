# Architecture Update 33

## Fixes

- Fixed `RoleAssignmentOption` sorting in `SecurityResolvers.CreateRoleDialog`: the record exposes `label`, not `Label`.
- Startup restore/import behavior is now explicit and mutually exclusive:
  - if `dump.json` is restored, `DatabaseInitializer.InitializeAsync(..., importSystemData: false)` is invoked only for local/idempotent bootstrap reconciliation;
  - `SystemData:Source`, `SYSTEMDATA_SOURCE`, and the GitHub-backed `SystemDataImporter` path are not evaluated/executed in the restored-from-dump branch;
  - external SystemData import is considered only when no dump was restored.
- `EffectiveAccess` is still rebuilt after either startup path.
