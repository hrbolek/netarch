# Architecture Update 27 — DigitalProcesses RBAC compilation

## Goal

Keep exactly one runtime authorization path:

`secured_entity.rbacobject_id -> EffectiveAccess -> RoleTypeOperation`

No additional workflow-time authorization predicate is evaluated by `SecurityLayer.Query<TEntity>()`.
Workflow state changes compile their access policy into RBAC source data (`Role` + `RbacAccessBinding`) and then rebuild `EffectiveAccess`.

## Core Security changes

### Group.inherit_roles

`GroupModel` now contains `inherit_roles` (default `true`).

When `false`, the Group is an inheritance boundary:
- roles assigned directly to the Group still apply to that Group;
- roles from ancestors do not flow into the Group or its descendants;
- descendants may receive permissions from their own Roles or AccessBindings.

DigitalProcesses creates Request, Submission and own FormNode security scopes with `inherit_roles=false`.

### RbacAccessBinding

New source model:

- `source_group_id`
- `source_role_type_id`
- `target_rbacobject_id`
- `target_role_type_id`
- validity interval
- optional `managed_scope_id`

It contains no user id.

Semantics:

> whoever currently holds `source_role_type_id` on `source_group_id` or any ancestor of that source group receives `target_role_type_id` on the exact target RBAC object.

Bindings are not recursively chained. They are compiled directly from ordinary Role rows.

### EffectiveAccess

`EffectiveAccessModel` gains `source_access_binding_id`.
`SecurityProjectionService` now compiles:

1. Role -> Group tree (respecting `inherit_roles` boundaries),
2. Role + Membership -> User security objects,
3. Role + RbacAccessBinding -> exact target RBAC object.

`SecurityLayer.Query<TEntity>()` is unchanged.

## DigitalProcesses changes

### Request initiator

`RequestModel.initiator_user_id` is explicit and immutable process metadata.
It is not represented by a submitted form value.

`ProcessDefinitionModel.initiator_role_type_id` determines which source role is assigned to the initiator on the Request RBAC group.

### RBAC hierarchy

Expected hierarchy:

```
FacultyRequestsRBACGroup
└── Process RBAC Group
    └── Request R1 Group [inherit_roles=false]
        ├── Submission S1 Group [inherit_roles=false]
        │   └── restricted section scope [inherit_roles=false]
        ├── Submission S2 Group [inherit_roles=false]
        └── ...
```

Domain parentage remains stored in Request/Submission/SubmissionNode foreign keys. The RBAC tree exists only for authorization.

### FormNode own security scope

`FormNodeModel.own_security_scope`:

- `false` -> SubmissionNode shares its inherited RBAC scope,
- `true` -> each materialized occurrence gets a new RBAC Group with `inherit_roles=false`.

A role/binding on the Submission does not leak into such a protected section.

### WorkflowStateAccessRule

New definition entity:

- `workflow_state_id`
- `source_role_type_id`
- `target_role_type_id`
- `target_scope`: `request`, `submission`, `form-node`
- optional `target_form_node_id`

`form-node` targets require `FormNode.own_security_scope=true`.

At state entry these rules create `RbacAccessBinding` rows.

Source group is always the current Request RBAC group. Therefore both cases resolve naturally:

- Dean role assigned on `FacultyRequestsRBACGroup` or Process ancestor,
- temporary Dean role assigned directly to one Request.

### Transition semantics

On transition:

1. current Submission is archived and remains immutable;
2. its RBAC groups and AccessBindings remain unchanged;
3. active-state bindings targeting the persistent Request group are expired;
4. a new Submission + new RBAC group are created;
5. SubmissionNode tree is cloned with new IDs/materialized paths;
6. `own_security_scope` nodes receive new RBAC groups;
7. target-state access rules create new bindings;
8. Request points to the new state/submission;
9. `EffectiveAccess` is rebuilt in the same transaction.

Historical Submission bindings are never rewritten during later transitions.

## New Dean test

Suppose historical Submission S2 contains a binding:

```
source_group = Request R1
source_role_type = Dean
target_rbacobject = Submission S2
target_role_type = FormReader
```

Initially:

```
User A -> Dean @ FacultyRequestsRBACGroup
```

Projection produces:

```
User A -> FormReader @ S2
```

Later the Role changes:

```
User A Dean role expires
User B -> Dean @ FacultyRequestsRBACGroup
```

RoleService rebuilds EffectiveAccess. The unchanged historical binding now produces:

```
User A -> no FormReader @ S2
User B -> FormReader @ S2
```

No historical Submission row or workflow record is modified.

The permissions UI now displays `source_access_binding_id` for diagnostic verification.

## Important configuration rule

Source RoleTypes such as `Dean` or the configured initiator role are best treated as functional/selector roles for state-sensitive access. Stable permissions may remain on them, but any operation that must vary by workflow state should come from the target operational RoleType created through a binding.

## Schema

This update changes DB schema:
- `groups.inherit_roles`
- `rbac_access_bindings`
- `effective_access.source_access_binding_id`
- `dp_form_nodes.own_security_scope`
- `dp_process_definitions.initiator_role_type_id`
- `dp_requests.initiator_user_id`
- `dp_workflow_state_access_rules`

With the current `EnsureCreated()` development strategy, recreate the SQLite database.
