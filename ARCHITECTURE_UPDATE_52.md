# Architecture Update 52 — Impact-based EffectiveAccess synchronization

This update removes full EffectiveAccess rebuilds from normal request/write paths.

## Runtime rule

Normal mutations now synchronize only their security impact:

- isolated secured object create -> exact RBAC object
- User/Membership change -> affected User RBAC object(s)
- Role create/update/delete -> descendants of the old/new role Group, member User scopes in those branches, and functional binding targets sourced from those branches
- Group hierarchy/inheritance change -> affected Group subtree + member User scopes + binding targets
- Digital Process Request start/transition -> Request security subtree

`RebuildAllAsync()` remains only for bootstrap/restore repair scenarios.

## Diff synchronization

Targeted synchronization no longer deletes projection rows and recreates them.
It computes the desired projection for the impacted RBAC objects and compares it with current EffectiveAccess rows:

- unchanged rows are untouched
- missing rows are inserted
- changed rows are updated
- no-longer-valid rows are expired by setting `valid_to_unix_ms`

This keeps normal authorization writes proportional to the actual security impact.

## Targeted source loading

For targeted synchronization the projection service no longer loads all Roles, Groups, Memberships and bindings.
It loads only:

- requested Group scopes
- Memberships whose User scope is requested
- bindings whose target scope is requested
- Group ancestors required by materialized paths
- Roles attached to those relevant ancestor groups

Full graph loading is reserved for explicit full rebuild.

## Tree impact

Materialized Group paths are used to identify affected descendants. Role/Group impacts include:

- descendant Group RBAC objects
- User RBAC objects reached by Memberships in the affected branch
- exact RBAC targets of access bindings whose source Group lies in the affected branch

This allows tree-local synchronization without rebuilding unrelated branches.

## Expired projection cleanup

Expired EffectiveAccess rows are not physically deleted in request hot paths.
`EffectiveAccessCleanupHostedService` periodically removes old expired projection rows.
Defaults:

- `SecurityProjection:CleanupIntervalMinutes = 360`
- `SecurityProjection:RetentionHours = 24`

Both are configurable.

## Startup

Core no longer rebuilds EffectiveAccess on every application startup.
A full rebuild happens only when:

- the projection is empty but security source data exists, or
- `SecurityProjection:RebuildOnStartup=true` is explicitly configured.

A database dump containing a valid projection can therefore restart without a global authorization rebuild.

## Remaining repair API

`RebuildAllAsync()` is intentionally retained as an administrative/bootstrap repair operation. It is no longer used by ordinary entity, role, membership, group, or Digital Processes mutations.
