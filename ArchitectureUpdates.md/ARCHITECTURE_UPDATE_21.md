# Architecture Update 21

## User security objects derived from membership

Runtime `Security.Query<TEntity>()` is intentionally unchanged.

`SecurityProjectionService` now materializes two kinds of `EffectiveAccess` rows from a Role over a Group:

1. Group security objects in the role group's subtree.
2. User security objects for users having Membership in that subtree, based on the invariant `User.rbacobject_id == User.id`.

For user-derived access the effective validity interval is the intersection of Role validity and Membership validity.

`EffectiveAccessModel` now carries `source_membership_id`. It is `Guid.Empty` for group-derived rows and the Membership id for user-derived rows. The composite key includes it so multiple memberships do not collide.

Membership C/U/D now rebuilds the projection in the same transaction as the source mutation.

## Role MediumCard

`RoleMediumCard` now displays:
- valid from
- valid to / without end
- deputy flag
- delegated-by role
- created timestamp
- updated timestamp

## Database schema

Because the `effective_access` composite key and schema changed, a database created through `EnsureCreated()` must be recreated for this update.
