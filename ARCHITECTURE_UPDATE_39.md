# Architecture Update 39 — RBAC-aware UI actions

## Goal

CUD actions should not invite users into an operation that is already known to be forbidden. The backend remains authoritative, but the UI renders actions according to the same RBAC decision.

## Add role action

`EntityPermissions` no longer renders an unconditional **Add role** button and modal. The card header lazy-loads a security-owned action fragment:

`GET /architecture/RbacPermissionResponse/create-role-action/{rbacobjectId}`

`SecurityResolvers.CreateRoleAction` evaluates `SecurityOperations.Create` for the current actor and target RBAC object.

- Allowed: renders an active Bootstrap button and modal whose content is loaded lazily.
- Denied: renders a disabled button with an explanatory tooltip/title. No modal loading URL is present in the returned HTML.

The dialog endpoint itself now repeats the RBAC `create` check and returns `403` on direct unauthorized access. The POST endpoint retains its existing authorization check as the final security boundary.

This gives three layers:

1. UI capability — prevent an operation that is already known to be unavailable.
2. Dialog/operation endpoint — reject direct URL access.
3. Mutation service/endpoint — authoritative check before state change.

## General pattern

The same pattern should be applied to other CUD UI actions. For ordinary existing entities, update/delete capabilities are evaluated against the entity's `rbacobject_id`. Create capabilities must use the trusted creation scope (`contained`, `typed`, or domain-specific `start`) introduced in Update 35 rather than a client-supplied future RBAC object.

A disabled UI element is preferred over silently hiding the action when discoverability is useful, because it communicates that the feature exists but is unavailable to the current actor.
