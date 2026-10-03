# Architecture Update 14 — HK2026 system-data initialization

Update 14 initializes the application's persistence model from the `_uois`
`systemdata.hk2026.json` dataset on the first database startup.

## Source

Default source:

`https://raw.githubusercontent.com/hrbolek/_uois/latest/systemdata.hk2026.json`

Override it with:

`SYSTEMDATA_SOURCE=/path/to/systemdata.hk2026.json`

or configuration key:

`SystemData:Source`

The initializer imports only when the database does not yet contain a real
(non-technical-root) Group tree.

## Imported sections

The importer reads the sections used by DBLayer:

- `users` -> `UserModel`
- `groups` -> `GroupModel`
- `grouptypes` -> `GroupTypeModel`
- `roles` -> `RoleModel`
- `roletypes` -> `RoleTypeModel`
- `memberships` / `groupmemberships` -> `MembershipModel` when present

Unknown sections are ignored.

## Group hierarchy

Source root groups (`mastergroup_id == null`) are attached below the technical
`RbacWellKnown.RootGroupId`. `Group.path` is rebuilt as a materialized path.
`Group.rbac_id == Group.id` remains the invariant.

## GroupType hierarchy

The source dataset does not provide `parent_group_type_id`. The importer derives
candidate parent types from real Group parent-child edges. A GroupType for which
one unique parent type can be inferred gets that parent. Ambiguous or root types
are attached under `RbacWellKnown.RootGroupTypeId`. Materialized paths are then
calculated for the type tree.

## RoleType -> Operation

The source dataset predates the normalized `RoleTypeOperation` model, so it
contains RoleTypes but no application-operation mapping. The initializer applies
a deliberately explicit bootstrap policy only when a RoleType has no mapping:

- `administrator` / `administrátor`: all known operations
- other RoleTypes: `read`, `list`

The mapping is ordinary database data and can later be administered without
changing schema or importer code.

## Compact seed extraction

`tools/extract_systemdata.py` can make a small seed file from the original large
JSON:

```bash
python tools/extract_systemdata.py systemdata.hk2026.json data/systemdata.seed.json
```

Then initialize with:

```bash
SYSTEMDATA_SOURCE=data/systemdata.seed.json
```

This is useful for offline Docker builds and tests.
