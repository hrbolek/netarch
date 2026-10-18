# Architecture Update 20

## RBAC object naming

The secured entity contract is now consistently named `rbacobject_id` instead of `rbac_id` across:

- DB models and DTO contracts
- create/update request models
- `EffectiveAccessModel`
- SecurityLayer queries and operation enrichment
- EF Core mappings, keys and indexes
- Razor permission bindings
- system-data import

The physical DB column is now also `rbacobject_id`.

`SystemDataImporter` reads source JSON `rbacobject_id` directly for imported secured entities, with compatibility fallbacks for older input that does not contain it.

## User model

`DBLayer.UserModel` now contains:

- `email : string?`
- `surname : string?`

The fields are propagated through `UserResponse` and mapped by EF Core to `users.email` and `users.surname`.

For system-data import:

- `email` is read from JSON `email`
- `surname` is read from JSON `surname`, with `familyname` as a fallback

Bootstrap fake-Entra users use the existing seed e-mail value.

## UI

`UserMediumCard` now renders Name, Surname and E-mail in addition to ID.

## Database compatibility

Because this update renames persisted `rbac_id` columns to `rbacobject_id` and adds `users.email` / `users.surname`, an existing SQLite database created through `EnsureCreated()` is not upgraded automatically. Recreate the prototype database or introduce an EF Core migration before preserving existing data across this update.
