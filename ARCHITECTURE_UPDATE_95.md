# ZArchitecture 95 — Process owns one form and one workflow

## Contract
- `ProcessDefinitionModel` now has stable `form_definition_id` and `workflow_definition_id` references; both have unique indexes, preventing two processes from declaring ownership of the same definition.
- `ProcessDefinitionVersion` keeps referencing exact `FormVersion` and `WorkflowVersion` IDs, but the service now checks that these versions belong to the process-owned definitions both on **creation** and **publication**.
- Creating a process atomically stages: RBAC group for process, nested RBAC groups for form/workflow, both definitions, initial draft versions of both, and initial draft **process version 1**. All in one database transaction, followed by RBAC projection synchronization. Newly created child objects are staged with the scoped `IDBLayer` as *trusted provisioning*, after authorization at the existing process creation catalog; checking EffectiveAccess for not-yet-created groups would be impossible.
- `/digital-processes/ProcessDefinitionResponse/view/{id}` now uses `DigitalProcessLargeCard`, with links to the owned form and workflow. The UI remains Razor-based, not hand-built HTML.

## Existing database IMPORTANT
The current models intentionally have nullable FK properties for **legacy migration**. New entities are always created with both values present. Existing rows are NOT automatically assigned to a random form or workflow.

1. Back up your DB.
2. Run `migrations/ARCHITECTURE_95_postgresql.sql` for existing PostgreSQL installations, or `migrations/ARCHITECTURE_95_sqlite.sql` for existing SQLite installations. For fresh SQLite DBs, EF Core can create full schema.
3. Audit existing processes and all their historical versions. If historical versions reference multiple distinct form/workflow definitions, decide manually how to consolidate or split historical processes; automatic selection is unsafe.
4. Assign both owned IDs to each existing process after normalization. Otherwise `CreateContainedAsync` / `PublishAsync` deliberately reject the unmigrated definition.
5. SQLite existing-table migrations above do not retrofit FK constraints; rebuilding tables or fresh DB required for true database-level FK validation.

## Validation
Source edits and archive structure were checked, but `dotnet` is unavailable in this environment; no compile or integration tests have run. In particular test EF Core insert order / transaction management with the configured database provider.
