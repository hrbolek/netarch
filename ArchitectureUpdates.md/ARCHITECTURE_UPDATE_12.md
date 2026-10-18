# Architecture update 12

## Normalized authorization operations

`RoleType.operations_csv` has been removed from the EF model. Authorization operations are now normalized:

- `OperationModel` -> table `operations`
- `RoleTypeOperationModel` -> table `role_type_operations`
- `RoleTypeModel` -> table `role_types`

The effective authorization path is now:

`EffectiveAccess.role_type_id -> RoleTypeOperation.role_type_id -> Operation`

Adding a new operation therefore requires inserting/registering an operation row, not changing a RoleType schema or CSV string.

## Role type foreign key

`RoleModel.role_type` string has been replaced in persistence by:

`RoleModel.role_type_id -> RoleTypeModel.id`

DTO output may still expose the readable role type name, but authorization and persistence joins use GUID foreign keys.

`EffectiveAccessModel` likewise stores `role_type_id` rather than the role type name.

## Security query

The secured `IQueryable<TEntity>` filter no longer parses CSV values. It translates the authorization test through normalized relations:

`Entity.rbac_id -> EffectiveAccess -> RoleTypeOperation -> Operation.name`

This keeps the filter SQL-composable for HTML, API and future OData/Excel access.

## RoleType create/update

RoleType create/update requests continue to accept operation names. `EfRoleTypeStore` resolves those names against the operation catalog and synchronizes `role_type_operations`.

Unknown operation names are rejected. Operations should be registered in the catalog first.

## Seed

The initializer now seeds the operation catalog first, then RoleTypes and their M:N mappings, and finally Roles referencing `role_type_id`.

## Database compatibility

This update changes the schema. `EnsureCreated` does not migrate an existing database from update 11. During prototype development delete the old `data/application.db` before first run of update 12, or introduce an EF migration before preserving existing data becomes necessary.
