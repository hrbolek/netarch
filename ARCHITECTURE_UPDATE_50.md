# Architecture Update 50

## Digital Processes create robustness

`SecurityProjectionService.RebuildAllAsync` now purges `effective_access` with a set-based EF Core `ExecuteDeleteAsync` through `IDBLayer.DeleteWhereAsync` instead of loading and tracking every row and issuing individual DELETE statements. Before inserting the rebuilt projection, rows are deduplicated by the actual `effective_access` composite key.

This keeps the derived authorization projection atomic with the surrounding transaction while avoiding the noisy per-row DELETE pattern seen during Digital Processes create operations.

## ProcessDefinition initiator role type

`CreateProcessDefinitionRequest.initiator_role_type_id` is now optional. When omitted, Digital Processes uses the well-known role type:

- ID: `05a3e0f5-f71e-4caa-8012-229d868aa8ca`
- semantic name: `já`

The standard HTML create form therefore asks only for name and description. Advanced/API callers may still explicitly supply another initiator role type.

## Digital Processes list titles

`DigitalProcessListPage<TModel>` now renders a visible page heading, for example:

- Workflow versions
- Workflow transitions
- Process definitions
- Form nodes

The browser page title uses the same label.
